using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.IO;
using System.Management.Automation;

namespace MABAppTecnologia.Policies
{
    /// <summary>
    /// Políticas de resiliencia centralizadas usando Polly
    /// Proporciona reintentos, circuit breakers y timeouts para operaciones críticas
    /// </summary>
    public static class ResiliencePolicies
    {
        /// <summary>
        /// Política de reintentos para operaciones de PowerShell con backoff exponencial
        /// Reintenta 3 veces con delays de 1s, 2s, 4s
        /// </summary>
        public static ResiliencePipeline<T> PowerShellRetryPolicy<T>(Action<Exception, TimeSpan, int>? onRetry = null)
        {
            return new ResiliencePipelineBuilder<T>()
                .AddRetry(new RetryStrategyOptions<T>
                {
                    ShouldHandle = new PredicateBuilder<T>()
                        .Handle<RuntimeException>()
                        .Handle<InvalidOperationException>()
                        .Handle<TimeoutException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        onRetry?.Invoke(args.Outcome.Exception!, args.RetryDelay, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Política de reintentos para operaciones de archivos
        /// Reintenta 5 veces con delay lineal para manejar locks temporales
        /// </summary>
        public static ResiliencePipeline<T> FileOperationRetryPolicy<T>(Action<Exception, TimeSpan, int>? onRetry = null)
        {
            return new ResiliencePipelineBuilder<T>()
                .AddRetry(new RetryStrategyOptions<T>
                {
                    ShouldHandle = new PredicateBuilder<T>()
                        .Handle<IOException>()
                        .Handle<UnauthorizedAccessException>(),
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Linear,
                    OnRetry = args =>
                    {
                        onRetry?.Invoke(args.Outcome.Exception!, args.RetryDelay, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Política de timeout para instalación de software
        /// Timeout de 5 minutos para instalaciones
        /// </summary>
        public static ResiliencePipeline<T> SoftwareInstallationTimeoutPolicy<T>()
        {
            return new ResiliencePipelineBuilder<T>()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(5)
                })
                .Build();
        }

        /// <summary>
        /// Política combinada: Timeout + Reintentos para operaciones de instalación
        /// </summary>
        public static ResiliencePipeline<T> SoftwareInstallationPolicy<T>(Action<Exception, TimeSpan, int>? onRetry = null)
        {
            return new ResiliencePipelineBuilder<T>()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(5)
                })
                .AddRetry(new RetryStrategyOptions<T>
                {
                    ShouldHandle = new PredicateBuilder<T>()
                        .Handle<TimeoutException>()
                        .Handle<InvalidOperationException>()
                        .Handle<IOException>(),
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(3),
                    BackoffType = DelayBackoffType.Constant,
                    OnRetry = args =>
                    {
                        onRetry?.Invoke(args.Outcome.Exception!, args.RetryDelay, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Circuit Breaker para operaciones repetitivas que fallan
        /// Se abre después de 3 fallas consecutivas y permanece abierto 30 segundos
        /// </summary>
        public static ResiliencePipeline<T> CircuitBreakerPolicy<T>(Action<CircuitBreakerStateChange>? onBreak = null)
        {
            return new ResiliencePipelineBuilder<T>()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
                {
                    FailureRatio = 0.5, // 50% de fallas
                    MinimumThroughput = 3, // Mínimo 3 intentos antes de evaluar
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder<T>()
                        .Handle<Exception>(),
                    OnOpened = args =>
                    {
                        onBreak?.Invoke(new CircuitBreakerStateChange(CircuitState.Open, args.Outcome.Exception));
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        onBreak?.Invoke(new CircuitBreakerStateChange(CircuitState.Closed, null));
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        onBreak?.Invoke(new CircuitBreakerStateChange(CircuitState.HalfOpen, null));
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Política general para operaciones de sistema
        /// Combina reintentos con timeout
        /// </summary>
        public static ResiliencePipeline<T> SystemOperationPolicy<T>(Action<Exception, TimeSpan, int>? onRetry = null)
        {
            return new ResiliencePipelineBuilder<T>()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(2)
                })
                .AddRetry(new RetryStrategyOptions<T>
                {
                    ShouldHandle = new PredicateBuilder<T>()
                        .Handle<RuntimeException>()
                        .Handle<InvalidOperationException>()
                        .Handle<IOException>()
                        .Handle<UnauthorizedAccessException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        onRetry?.Invoke(args.Outcome.Exception!, args.RetryDelay, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }

    /// <summary>
    /// Representa un cambio de estado en un circuit breaker
    /// </summary>
    public record CircuitBreakerStateChange(CircuitState State, Exception? Exception);

    /// <summary>
    /// Estados posibles del circuit breaker
    /// </summary>
    public enum CircuitState
    {
        Closed,    // Operando normalmente
        Open,      // Bloqueado por fallas
        HalfOpen   // Probando si se recuperó
    }
}
