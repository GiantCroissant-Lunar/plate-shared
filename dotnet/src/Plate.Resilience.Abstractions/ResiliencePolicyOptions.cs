using System;

namespace Plate.Resilience;

public enum FallbackBehavior
{
    None = 0,
    ReturnDefault = 1,
}

public sealed class ResiliencePolicyOptions
{
    public int RetryCount { get; set; } = 3;

    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public bool UseExponentialBackoff { get; set; } = true;

    public int CircuitBreakerAllowedFailures { get; set; } = 0;

    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan? Timeout { get; set; }

    public FallbackBehavior FallbackBehavior { get; set; } = FallbackBehavior.None;

    public Action<int, TimeSpan, Exception>? OnRetry { get; set; }

    public Action<Exception>? OnFailure { get; set; }

    public Action<Exception, TimeSpan>? OnCircuitBreak { get; set; }

    public Action? OnCircuitReset { get; set; }

    public Action? OnCircuitHalfOpen { get; set; }
}
