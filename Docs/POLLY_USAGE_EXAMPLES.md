# Guía de Uso de Polly para Resiliencia

## Políticas Implementadas

### 1. PowerShellRetryPolicy<T>
**Uso**: Operaciones de PowerShell con backoff exponencial
**Reintentos**: 3 veces (1s, 2s, 4s)
**Excepciones manejadas**: RuntimeException, InvalidOperationException, TimeoutException

### 2. FileOperationRetryPolicy<T>
**Uso**: Operaciones de archivos que pueden tener locks temporales
**Reintentos**: 5 veces (delay lineal de 500ms)
**Excepciones manejadas**: IOException, UnauthorizedAccessException

### 3. SoftwareInstallationPolicy<T>
**Uso**: Instalación de software con timeout y reintentos
**Timeout**: 5 minutos
**Reintentos**: 2 veces (3 segundos entre intentos)

### 4. CircuitBreakerPolicy<T>
**Uso**: Operaciones repetitivas que fallan frecuentemente
**Apertura**: Después de 50% de fallas en 3 intentos
**Duración abierto**: 30 segundos

### 5. SystemOperationPolicy<T>
**Uso**: Operaciones generales de sistema
**Timeout**: 2 minutos
**Reintentos**: 3 veces con backoff exponencial

---

## Ejemplos de Uso

### Ejemplo 1: Renombrar Computadora con Reintentos

```csharp
using MABAppTecnologia.Policies;

public OperationResult RenameComputer(string newName)
{
    var policy = ResiliencePolicies.PowerShellRetryPolicy<OperationResult>(
        onRetry: (ex, delay, attempt) =>
        {
            _logService.LogWarning($"Reintento {attempt} después de {delay.TotalSeconds}s: {ex.Message}");
        }
    );

    return policy.Execute(() =>
    {
        _logService.LogInfo($"Intentando renombrar equipo a: {newName}");

        using var ps = PowerShell.Create();
        ps.AddCommand("Rename-Computer")
          .AddParameter("NewName", newName)
          .AddParameter("Force");

        var results = ps.Invoke();

        if (ps.HadErrors)
        {
            var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
            throw new InvalidOperationException($"Error al renombrar: {errors}");
        }

        _logService.LogSuccess($"Equipo renombrado exitosamente a: {newName}");
        return OperationResult.Ok($"Equipo renombrado a {newName}");
    });
}
```

### Ejemplo 2: Limpiar Iconos del Escritorio con Política de Archivos

```csharp
public OperationResult CleanDesktopIcons()
{
    var policy = ResiliencePolicies.FileOperationRetryPolicy<OperationResult>(
        onRetry: (ex, delay, attempt) =>
        {
            _logService.LogWarning($"Archivo bloqueado, reintentando {attempt}/5...");
        }
    );

    return policy.Execute(() =>
    {
        _logService.LogInfo("Limpiando iconos del escritorio...");

        var desktopPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        foreach (var desktopPath in desktopPaths)
        {
            if (Directory.Exists(desktopPath))
            {
                var shortcuts = Directory.GetFiles(desktopPath, "*.lnk");
                foreach (var shortcut in shortcuts)
                {
                    File.Delete(shortcut); // Puede lanzar IOException si está bloqueado
                }
            }
        }

        _logService.LogSuccess("Iconos del escritorio limpiados");
        return OperationResult.Ok("Iconos del escritorio eliminados exitosamente");
    });
}
```

### Ejemplo 3: Instalación de Software con Timeout y Reintentos

```csharp
public async Task<OperationResult> InstallSoftware(SoftwareItem software)
{
    var policy = ResiliencePolicies.SoftwareInstallationPolicy<OperationResult>(
        onRetry: (ex, delay, attempt) =>
        {
            if (_logService is IStructuredLogService structuredLog)
            {
                structuredLog.LogWarning(
                    "Reintento de instalación {Software}: intento {Attempt} después de {Delay}s",
                    software.Name,
                    attempt,
                    delay.TotalSeconds);
            }
        }
    );

    return await policy.ExecuteAsync(async (ct) =>
    {
        _logService.LogInfo($"Iniciando instalación: {software.Name}");

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = software.InstallerPath,
            Arguments = software.SilentArgs,
            UseShellExecute = false
        });

        if (process == null)
            throw new InvalidOperationException($"No se pudo iniciar instalador: {software.Name}");

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Instalación falló con código: {process.ExitCode}");

        _logService.LogSuccess($"Software instalado: {software.Name}");
        return OperationResult.Ok($"{software.Name} instalado correctamente");

    }, CancellationToken.None);
}
```

### Ejemplo 4: Operación con Circuit Breaker

```csharp
private readonly ResiliencePipeline<OperationResult> _circuitBreaker;

public SystemService(ILogService logService)
{
    _logService = logService;

    // Inicializar circuit breaker
    _circuitBreaker = ResiliencePolicies.CircuitBreakerPolicy<OperationResult>(
        onBreak: (change) =>
        {
            if (change.State == CircuitState.Open)
            {
                _logService.LogError($"Circuit Breaker ABIERTO: {change.Exception?.Message}");
            }
            else if (change.State == CircuitState.Closed)
            {
                _logService.LogInfo("Circuit Breaker CERRADO: Sistema recuperado");
            }
        }
    );
}

public OperationResult RiskyOperation()
{
    return _circuitBreaker.Execute(() =>
    {
        // Operación que puede fallar frecuentemente
        // Si falla >50% de las veces en 3 intentos, el circuito se abre
        // Durante 30 segundos, todas las llamadas fallarán inmediatamente
        // sin intentar la operación

        return PerformRiskyTask();
    });
}
```

### Ejemplo 5: Combinar Múltiples Políticas

```csharp
public async Task<OperationResult> ComplexOperation()
{
    // Combinar timeout, reintentos y circuit breaker
    var pipeline = new ResiliencePipelineBuilder<OperationResult>()
        .AddTimeout(TimeSpan.FromMinutes(2))
        .AddRetry(new RetryStrategyOptions<OperationResult>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                _logService.LogWarning($"Reintento {args.AttemptNumber}: {args.Outcome.Exception?.Message}");
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    return await pipeline.ExecuteAsync(async (ct) =>
    {
        // Operación compleja que puede:
        // - Exceder timeout
        // - Requerir reintentos
        // - Necesitar circuit breaker

        await Task.Delay(100, ct);
        return OperationResult.Ok("Operación compleja exitosa");
    }, CancellationToken.None);
}
```

---

## Patrones Recomendados

### 1. Log en cada reintento
```csharp
var policy = ResiliencePolicies.PowerShellRetryPolicy<T>(
    onRetry: (ex, delay, attempt) =>
    {
        if (_logService is IStructuredLogService structuredLog)
        {
            structuredLog.LogWarning(
                "Reintento {Attempt} después de {Delay}ms: {Error}",
                attempt,
                delay.TotalMilliseconds,
                ex.Message);
        }
    }
);
```

### 2. Usar políticas específicas para cada tipo de operación
- **PowerShell** → `PowerShellRetryPolicy`
- **Archivos** → `FileOperationRetryPolicy`
- **Software** → `SoftwareInstallationPolicy`
- **General** → `SystemOperationPolicy`

### 3. Registrar estado del Circuit Breaker
```csharp
ResiliencePolicies.CircuitBreakerPolicy<T>(
    onBreak: (change) =>
    {
        structuredLog.LogInformation(
            "Circuit Breaker cambió a {State}: {Error}",
            change.State,
            change.Exception?.Message ?? "N/A");
    }
);
```

---

## Beneficios de la Implementación

1. **Resiliencia**: Las operaciones se recuperan automáticamente de fallas transitorias
2. **Visibilidad**: Logs estructurados muestran cada reintento y su razón
3. **Control de Carga**: Circuit breakers previenen sobrecarga del sistema
4. **Timeouts**: Evitan operaciones colgadas indefinidamente
5. **Experiencia de Usuario**: Reintentos automáticos vs. errores inmediatos

---

## Migración Gradual

Para migrar código existente a usar Polly:

**Antes:**
```csharp
public OperationResult DoSomething()
{
    try
    {
        // Operación riesgosa
        return OperationResult.Ok("Éxito");
    }
    catch (Exception ex)
    {
        _logService.LogError("Error", ex);
        return OperationResult.Fail("Error", ex.Message, ex);
    }
}
```

**Después:**
```csharp
public OperationResult DoSomething()
{
    var policy = ResiliencePolicies.SystemOperationPolicy<OperationResult>(
        onRetry: (ex, delay, attempt) =>
        {
            _logService.LogWarning($"Reintento {attempt}: {ex.Message}");
        }
    );

    return policy.Execute(() =>
    {
        // Operación riesgosa (lanza excepciones si falla)
        // La política manejará reintentos y timeouts

        return OperationResult.Ok("Éxito");
    });
}
```

---

## Referencias

- [Polly Documentation](https://www.pollydocs.org/)
- [Resilience Patterns](https://learn.microsoft.com/en-us/azure/architecture/patterns/category/resiliency)
- ResiliencePolicies.cs - Implementación de políticas centralizadas
