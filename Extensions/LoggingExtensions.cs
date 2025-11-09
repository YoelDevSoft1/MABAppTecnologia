using MABAppTecnologia.Services;
using MABAppTecnologia.Models;

namespace MABAppTecnologia.Extensions
{
    /// <summary>
    /// Extensiones para facilitar el uso de logging estructurado
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Ejecuta una operación y registra el resultado automáticamente
        /// </summary>
        public static OperationResult LogAndExecute(
            this ILogService logService,
            string operationName,
            Func<OperationResult> operation)
        {
            try
            {
                logService.LogInfo($"Ejecutando: {operationName}");
                var result = operation();

                if (result.Success)
                {
                    logService.LogSuccess($"✓ {operationName}: {result.Message}");
                }
                else
                {
                    logService.LogError($"✗ {operationName}: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                logService.LogError($"Excepción en {operationName}", ex);
                return OperationResult.Fail(operationName, ex.Message, ex);
            }
        }

        /// <summary>
        /// Ejecuta una operación async y registra el resultado automáticamente
        /// </summary>
        public static async Task<OperationResult> LogAndExecuteAsync(
            this ILogService logService,
            string operationName,
            Func<Task<OperationResult>> operation)
        {
            try
            {
                logService.LogInfo($"Ejecutando async: {operationName}");
                var result = await operation();

                if (result.Success)
                {
                    logService.LogSuccess($"✓ {operationName}: {result.Message}");
                }
                else
                {
                    logService.LogError($"✗ {operationName}: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                logService.LogError($"Excepción en {operationName}", ex);
                return OperationResult.Fail(operationName, ex.Message, ex);
            }
        }

        /// <summary>
        /// Log de inicio de paso con formato consistente
        /// </summary>
        public static void LogStepStart(this ILogService logService, int stepNumber, string stepName)
        {
            logService.LogInfo($"═══════════════════════════════════════════════════");
            logService.LogInfo($"PASO {stepNumber}: {stepName}");
            logService.LogInfo($"═══════════════════════════════════════════════════");
        }

        /// <summary>
        /// Log de finalización de paso con resumen
        /// </summary>
        public static void LogStepComplete(
            this ILogService logService,
            int stepNumber,
            string stepName,
            int successCount,
            int totalCount)
        {
            var successRate = totalCount > 0 ? (successCount * 100.0 / totalCount) : 0;
            logService.LogSuccess($"PASO {stepNumber} COMPLETADO: {stepName}");
            logService.LogInfo($"Resultado: {successCount}/{totalCount} operaciones exitosas ({successRate:F1}%)");
            logService.LogInfo($"───────────────────────────────────────────────────");
        }

        /// <summary>
        /// Log de progreso de instalación
        /// </summary>
        public static void LogInstallationProgress(
            this ILogService logService,
            string softwareName,
            int current,
            int total)
        {
            logService.LogInfo($"[{current}/{total}] Instalando: {softwareName}");
        }

        /// <summary>
        /// Log de configuración de sistema con valores
        /// </summary>
        public static void LogConfiguration(
            this ILogService logService,
            string settingName,
            object value)
        {
            if (logService is IStructuredLogService structuredLog)
            {
                structuredLog.LogInformation("Configuración aplicada: {SettingName} = {Value}",
                    settingName, value);
            }
            else
            {
                logService.LogInfo($"Configuración aplicada: {settingName} = {value}");
            }
        }

        /// <summary>
        /// Log de usuario creado/modificado
        /// </summary>
        public static void LogUserOperation(
            this ILogService logService,
            string operation,
            string username,
            bool success,
            string? details = null)
        {
            if (logService is IStructuredLogService structuredLog)
            {
                if (success)
                {
                    structuredLog.LogInformation("Usuario {Operation}: {Username}. Detalles: {Details}",
                        operation, username, details ?? "N/A");
                }
                else
                {
                    structuredLog.LogError(null, "Error en usuario {Operation}: {Username}. Error: {Details}",
                        operation, username, details ?? "Unknown");
                }
            }
            else
            {
                var message = $"Usuario {operation}: {username}";
                if (details != null)
                    message += $" - {details}";

                if (success)
                    logService.LogSuccess(message);
                else
                    logService.LogError(message);
            }
        }
    }
}
