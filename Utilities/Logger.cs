using MelonLoader;

namespace CustomAlbums.Utilities
{
    public class Logger
    {
        private readonly MelonLogger.Instance _logger;

        public Logger(string className)
        {
            _logger = new MelonLogger.Instance(className);
        }

        public void Msg(string message, bool verbose = true)
        {
            if (verbose && !ModSettings.VerboseLogging) return;
            _logger.Msg(message);
        }

        public void Warning(string message)
        {
            _logger.Msg(ConsoleColor.Yellow, "Warning: " + message);
        }

        public void Error(string message)
        {
            _logger.Msg(ConsoleColor.Red, "ERROR: " + message);
        }
    }
}
