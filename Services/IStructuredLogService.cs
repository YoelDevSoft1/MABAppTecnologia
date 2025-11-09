using System.Runtime.CompilerServices;

namespace MABAppTecnologia.Services
{
    /// <summary>
    /// Interfaz extendida para logging estructurado con Serilog
    /// </summary>
    public interface IStructuredLogService : ILogService
    {
        // Métodos de logging estructurado con parámetros tipados
        void LogInformation(string messageTemplate, params object[] propertyValues);
        void LogWarning(string messageTemplate, params object[] propertyValues);
        void LogError(Exception? exception, string messageTemplate, params object[] propertyValues);
        void LogDebug(string messageTemplate, params object[] propertyValues);

        // Contextos enriquecidos
        IDisposable BeginScope(string scopeName);

        // Logging de operaciones con medición de tiempo
        void LogOperation(string operationName, Action operation);
        T LogOperation<T>(string operationName, Func<T> operation);
        Task LogOperationAsync(string operationName, Func<Task> operation);
        Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation);

        // Logging de performance
        IDisposable MeasureOperation(string operationName, [CallerMemberName] string? callerName = null);
    }
}
