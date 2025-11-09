namespace MABAppTecnologia.Services
{
    public interface ILogService
    {
        void LogInfo(string message);
        void LogError(string message, Exception? ex = null);
        void LogSuccess(string message);
        void LogWarning(string message);
        void LogDebug(string message);
        string GetLogFilePath();
    }
}
