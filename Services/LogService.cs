using System.IO;

namespace MABAppTecnologia.Services
{
    public class LogService : ILogService
    {
        private readonly string _logFilePath;

        public LogService()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var logsFolder = Path.Combine(appPath, "Logs");
            Directory.CreateDirectory(logsFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logsFolder, $"MAB_Log_{timestamp}.txt");
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var errorMessage = ex != null ? $"{message} - Exception: {ex.Message}\n{ex.StackTrace}" : message;
            WriteLog("ERROR", errorMessage);
        }

        public void LogSuccess(string message)
        {
            WriteLog("SUCCESS", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Si falla el logging, no hacer nada para evitar romper la aplicación
            }
        }

        public string GetLogFilePath() => _logFilePath;
    }
}
