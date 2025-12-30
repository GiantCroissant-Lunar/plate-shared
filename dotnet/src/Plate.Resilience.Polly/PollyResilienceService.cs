using System;
using System.Threading;
using System.Threading.Tasks;
using Plate.Resilience;
using Polly;

namespace Plate.Resilience.Polly;

public sealed class PollyResilienceService : IResilienceService
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        ResiliencePolicyOptions? options = null,
        CancellationToken ct = default)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        options ??= new ResiliencePolicyOptions();

        var policy = BuildPolicy(options);

        try
        {
            await policy.ExecuteAsync(async token => await action(token).ConfigureAwait(false), ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            options.OnFailure?.Invoke(ex);
            throw;
        }
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResiliencePolicyOptions? options = null,
        CancellationToken ct = default)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        options ??= new ResiliencePolicyOptions();

        var policy = BuildPolicy<T>(options);

        try
        {
            return await policy.ExecuteAsync(async token => await action(token).ConfigureAwait(false), ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            options.OnFailure?.Invoke(ex);
            throw;
        }
    }

    private static IAsyncPolicy BuildPolicy(ResiliencePolicyOptions options)
    {
        IAsyncPolicy policy = Policy.NoOpAsync();

        if (options.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            policy = Policy.TimeoutAsync(timeout);
        }

        if (options.RetryCount > 0)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .WaitAndRetryAsync(
                    options.RetryCount,
                    attempt => GetRetryDelay(options, attempt),
                    (ex, delay, attempt, _) => options.OnRetry?.Invoke(attempt, delay, ex));

            policy = retryPolicy.WrapAsync(policy);
        }

        if (options.CircuitBreakerAllowedFailures > 0)
        {
            var circuitBreakerPolicy = Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .CircuitBreakerAsync(
                    options.CircuitBreakerAllowedFailures,
                    options.CircuitBreakerDuration,
                    (ex, duration) => options.OnCircuitBreak?.Invoke(ex, duration),
                    () => options.OnCircuitReset?.Invoke(),
                    () => options.OnCircuitHalfOpen?.Invoke());

            policy = circuitBreakerPolicy.WrapAsync(policy);
        }

        return policy;
    }

    private static IAsyncPolicy<T> BuildPolicy<T>(ResiliencePolicyOptions options)
    {
        IAsyncPolicy<T> policy = Policy.NoOpAsync<T>();

        if (options.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            policy = Policy.TimeoutAsync<T>(timeout);
        }

        if (options.RetryCount > 0)
        {
            var retryPolicy = Policy<T>
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .WaitAndRetryAsync(
                    options.RetryCount,
                    attempt => GetRetryDelay(options, attempt),
                    (result, delay, attempt, _) => options.OnRetry?.Invoke(
                        attempt,
                        delay,
                        result.Exception ?? new Exception("Polly retry triggered without exception.")));

            policy = retryPolicy.WrapAsync(policy);
        }

        if (options.CircuitBreakerAllowedFailures > 0)
        {
            var circuitBreakerPolicy = Policy<T>
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .CircuitBreakerAsync(
                    options.CircuitBreakerAllowedFailures,
                    options.CircuitBreakerDuration,
                    (result, duration) => options.OnCircuitBreak?.Invoke(
                        result.Exception ?? new Exception("Polly circuit breaker triggered without exception."),
                        duration),
                    () => options.OnCircuitReset?.Invoke(),
                    () => options.OnCircuitHalfOpen?.Invoke());

            policy = circuitBreakerPolicy.WrapAsync(policy);
        }

        if (options.FallbackBehavior == FallbackBehavior.ReturnDefault)
        {
            var fallbackPolicy = Policy<T>
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .FallbackAsync(
                    (_, _) => Task.FromResult(default(T)!),
                    (result, _) =>
                    {
                        if (result.Exception is { } ex)
                        {
                            options.OnFailure?.Invoke(ex);
                        }

                        return Task.CompletedTask;
                    });

            policy = fallbackPolicy.WrapAsync(policy);
        }

        return policy;
    }

    private static TimeSpan GetRetryDelay(ResiliencePolicyOptions options, int attempt)
    {
        if (!options.UseExponentialBackoff)
        {
            return options.RetryBaseDelay;
        }

        if (attempt <= 1)
        {
            return options.RetryBaseDelay;
        }

        var factor = 1d;
        for (var i = 1; i < attempt; i++)
        {
            factor *= 2d;
        }

        var delayMs = options.RetryBaseDelay.TotalMilliseconds * factor;
        if (delayMs < 0)
        {
            delayMs = 0;
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }
}
