using CustomAlbums.Data;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using NAudio.Vorbis;
using NLayer;
using UnityEngine;
using Action = Il2CppSystem.Action;
using Logger = CustomAlbums.Utilities.Logger;

// ReSharper disable AccessToModifiedClosure

namespace CustomAlbums.Managers
{
    public static class AudioManager
    {
        public const int AsyncReadSpeed = 4096;

        private static Coroutine _currentCoroutine;
        private static readonly Dictionary<string, Coroutine> Coroutines = new();
        private static readonly Logger Logger = new(nameof(AudioManager));

        public static bool SwitchLoad(string name)
        {
            if (!Coroutines.TryGetValue(name, out var routine)) return false;
            _currentCoroutine = routine;

            Logger.Msg($"Switching to async load of {name}");
            return true;
        }

        public static AudioClip LoadClipFromMp3(MemoryStream stream, string name)
        {
            var mp3 = new MpegFile(stream);
            var sampleCount = mp3.Length / sizeof(float);
            var audioClip =
                AudioClip.Create(name, (int)sampleCount / mp3.Channels, mp3.Channels, mp3.SampleRate, false);

            var remainingSamples = sampleCount;
            var index = 0;

            if (name.EndsWith("_music") && mp3.SampleRate != 44100)
                Logger.Warning(
                    $"{name}.mp3 is not 44.1khz, desyncs may occur! Consider switching to .ogg format or using 44.1khz");

            Coroutine coroutine = null;
            coroutine = CreateCoroutine((Il2CppSystem.Func<bool>)delegate
            {
                // Stop coroutine if the asset is unloaded
                if (audioClip == null)
                {
                    Coroutines.Remove(name);
                    if (_currentCoroutine == coroutine) _currentCoroutine = null;

                    Logger.Msg($"Aborting async load of {name}.mp3");
                    return true;
                }

                // Pause coroutine if it is not active
                if (coroutine != _currentCoroutine) return false;

                var sampleArray = new float[Math.Min(AsyncReadSpeed, remainingSamples)];
                var readCount = mp3.ReadSamples(sampleArray, 0, sampleArray.Length);

                audioClip.SetData(sampleArray, index / mp3.Channels);

                index += readCount;
                remainingSamples -= readCount;

                if (remainingSamples > 0 && readCount != 0) return false;

                mp3.Dispose();
                stream.Dispose();

                Coroutines.Remove(name);
                _currentCoroutine = null;

                Logger.Msg($"Finished async load of {name}.mp3");
                return true;
            });

            Coroutines[name] = coroutine;
            _currentCoroutine = coroutine;

            return audioClip;
        }

        public static AudioClip LoadClipFromOgg(MemoryStream stream, string name)
        {
            var ogg = new VorbisWaveReader(stream);
            var sampleCount = (int)(ogg.Length / (ogg.WaveFormat.BitsPerSample / 8));
            var audioClip = AudioClip.Create(name, sampleCount / ogg.WaveFormat.Channels, ogg.WaveFormat.Channels,
                ogg.WaveFormat.SampleRate, false);

            var remainingSamples = sampleCount;
            var index = 0;

            Coroutine coroutine = null;
            coroutine = CreateCoroutine((Il2CppSystem.Func<bool>)delegate
            {
                // Stop coroutine if the asset is unloaded
                if (audioClip == null)
                {
                    Coroutines.Remove(name);
                    if (_currentCoroutine == coroutine) _currentCoroutine = null;

                    Logger.Msg($"Aborting async load of {name}.ogg");
                    return true;
                }

                // Pause coroutine if it is not active
                if (coroutine != _currentCoroutine) return false;

                var sampleArray = new float[Math.Min(AsyncReadSpeed, remainingSamples)];
                var readCount = ogg.Read(sampleArray, 0, sampleArray.Length);

                try
                {
                    audioClip.SetData(sampleArray, index / ogg.WaveFormat.Channels);
                }
                catch (Exception e)
                {
                    if ((double)ogg.Position / 1000 > ogg.Length)
                        Logger.Warning(
                            "Possible over-read of audio file. This is a file-dependent anomaly. Consider making a minimal edit and re-saving.");
                    Logger.Error(
                        $"Exception while reading at offset {index} of {(double)ogg.Position / 1000 / ogg.Length} in {name}, aborting: {e.Message}");

                    ogg.Dispose();
                    stream.Dispose();

                    Coroutines.Remove(name);
                    _currentCoroutine = null;
                    return true;
                }

                index += readCount;
                remainingSamples -= readCount;

                if (remainingSamples > 0 && readCount != 0) return false;

                ogg.Dispose();
                stream.Dispose();

                Coroutines.Remove(name);
                _currentCoroutine = null;

                Logger.Msg($"Finished async load of {name}.ogg");
                return true;
            });

            Coroutines[name] = coroutine;
            _currentCoroutine = coroutine;

            return audioClip;
        }

        private static Coroutine CreateCoroutine(Il2CppSystem.Func<bool> update)
        {
            return SingletonMonoBehaviour<CoroutineManager>.instance.StartCoroutine(
                (Action)delegate { }, update);
        }

        /// <summary>
        ///     Gets the audio clip for the specified album.
        /// </summary>
        /// <param name="album">The specified album.</param>
        /// <param name="name">This is "music" or "demo". Defaults to "music".</param>
        /// <returns></returns>
        public static AudioClip GetAudio(this Album album, string name = "music")
        {
            var key = $"{album.AlbumName}_{name}";
            if (album.HasFile($"{name}.ogg"))
            {
                // Load music.ogg
                var stream = album.OpenMemoryStream($"{name}.ogg");
                return LoadClipFromOgg(stream, key);
            }

            if (album.HasFile($"{name}.mp3"))
            {
                // Load music.mp3
                var stream = album.OpenMemoryStream($"{name}.mp3");
                return LoadClipFromMp3(stream, key);
            }

            // No music file found
            Logger.Error($"Could not find audio file for {name} in {album.Info.Name}");
            return null;
        }
    }
}