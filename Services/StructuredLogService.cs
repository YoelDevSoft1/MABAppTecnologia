using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using Serilog;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace MABAppTecnologia.Services
{
    /// <summary>
    /// Implementación de logging estructurado usando Serilog
    /// Compatible con ILogService existente pero con capacidades avanzadas
    /// </summary>
    public class StructuredLogService : IStructuredLogService
    {
        private readonly ILogger _logger;
        private readonly string _logFilePath;

        public StructuredLogService()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var logsFolder = Path.Combine(appPath, "Logs");
            Directory.CreateDirectory(logsFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logsFolder, $"MAB_Log_{timestamp}.log");

            // Configurar Serilog con múltiples sinks y enrichers
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "MABAppTecnologia")
                .WriteTo.File(
                    path: _logFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true
                )
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _logger.Information("Serilog inicializado correctamente. Log file: {LogPath}", _logFilePath);
        }

        // ===== Implementación de ILogService (compatibilidad con código existente) =====

        public void LogInfo(string message)
        {
            _logger.Information(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                _logger.Error(ex, message);
            }
            else
            {
                _logger.Error(message);
            }
        }

        public void LogSuccess(string message)
        {
            // SUCCESS como nivel INFO con marcador especial
            using (LogContext.PushProperty("MessageType", "SUCCESS"))
            {
                _logger.Information(message);
            }
        }

        public void LogWarning(string message)
        {
            _logger.Warning(message);
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public string GetLogFilePath() => _logFilePath;

        // ===== Métodos estructurados avanzados =====

        public void LogInformation(string messageTemplate, params object[] propertyValues)
        {
            _logger.Information(messageTemplate, propertyValues);
        }

        public void LogWarning(string messageTemplate, params object[] propertyValues)
        {
            _logger.Warning(messageTemplate, propertyValues);
        }

        public void LogError(Exception? exception, string messageTemplate, params object[] propertyValues)
        {
            if (exception != null)
            {
                _logger.Error(exception, messageTemplate, propertyValues);
            }
            else
            {
                _logger.Error(messageTemplate, propertyValues);
            }
        }

        public void LogDebug(string messageTemplate, params object[] propertyValues)
        {
            _logger.Debug(messageTemplate, propertyValues);
        }

        public IDisposable BeginScope(string scopeName)
        {
            return LogContext.PushProperty("Scope", scopeName);
        }

        // ===== Logging de operaciones con medición de tiempo =====

        public void LogOperation(string operationName, Action operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Iniciando operación: {OperationName}", operationName);
                operation();
                sw.Stop();
                _logger.Information("Operación completada: {OperationName} en {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.Error(ex, "Operación fallida: {OperationName} después de {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public T LogOperation<T>(string operationName, Func<T> operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Iniciando operación: {OperationName}", operationName);
                var result = operation();
                sw.Stop();
                _logger.Information("Operación completada: {OperationName} en {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.Error(ex, "Operación fallida: {OperationName} después de {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task LogOperationAsync(string operationName, Func<Task> operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Iniciando operación async: {OperationName}", operationName);
                await operation();
                sw.Stop();
                _logger.Information("Operación async completada: {OperationName} en {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.Error(ex, "Operación async fallida: {OperationName} después de {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Iniciando operación async: {OperationName}", operationName);
                var result = await operation();
                sw.Stop();
                _logger.Information("Operación async completada: {OperationName} en {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.Error(ex, "Operación async fallida: {OperationName} después de {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public IDisposable MeasureOperation(string operationName, [CallerMemberName] string? callerName = null)
        {
            return new OperationTimer(_logger, operationName, callerName);
        }

        // ===== Clase helper para medición automática de tiempo =====

        private class OperationTimer : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _operationName;
            private readonly string? _callerName;
            private readonly Stopwatch _stopwatch;

            public OperationTimer(ILogger logger, string operationName, string? callerName)
            {
                _logger = logger;
                _operationName = operationName;
                _callerName = callerName;
                _stopwatch = Stopwatch.StartNew();

                _logger.Debug("Iniciando medición: {OperationName} desde {Caller}",
                    _operationName, _callerName ?? "Unknown");
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _logger.Information("Operación medida: {OperationName} desde {Caller} - Duración: {ElapsedMs}ms",
                    _operationName, _callerName ?? "Unknown", _stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
