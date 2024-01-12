
using System.Collections.ObjectModel;

namespace CustomAlbums.Utilities
{
    public static class Il2CppExtensions
    {
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this List<T> list)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.List<T>(list.Count);
            foreach (var item in list)
            {
                il2Cpp.Add(item);
            }

            return il2Cpp;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this IEnumerable<T> list)
        {
            var array = list.ToArray();
            var il2Cpp = new Il2CppSystem.Collections.Generic.List<T>(array.Length);
            foreach (var item in array)
            {
                il2Cpp.Add(item);
            }

            return il2Cpp;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this ReadOnlyCollection<T> collection)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.List<T>(collection.Count);
            foreach (var item in collection)
            {
                il2Cpp.Add(item);
            }

            return il2Cpp;
        }

        public static Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> ToIl2Cpp<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
            {
                il2Cpp.Add(key, value);
            }

            return il2Cpp;
        }

        public static List<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var managed = new List<T>(list.Count);
            foreach (var item in list)
            {
                managed.Add(item);
            }

            return managed;
        }

        public static Dictionary<TKey, T> ToManaged<TKey, T>(
            this Il2CppSystem.Collections.Generic.Dictionary<TKey, T> dictionary)
        {
            var managed = new Dictionary<TKey, T>(dictionary.Count);
            foreach (var entry in dictionary)
            {
                managed.Add(entry.Key, entry.Value);
            }

            return managed;
        }

        public static void AddManagedRange<T>(this Il2CppSystem.Collections.Generic.List<T> il2cpp, IEnumerable<T> managed)
        {
            var array = managed.ToArray();
            il2cpp.Capacity += array.Length;
            foreach (var item in array)
            {
                il2cpp.Add(item);
            }
        }

        public static bool TryGetValuePossibleNullKey<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dict, TKey key, out TValue outValue)
        {
            outValue = default;
            if (key == null || !dict.ContainsKey(key)) return false;
            outValue = dict[key];
            return true;
        }
    }
}
