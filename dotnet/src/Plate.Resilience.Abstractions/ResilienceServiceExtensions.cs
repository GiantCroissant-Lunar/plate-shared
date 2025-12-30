using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plate.Resilience;

public static class ResilienceServiceExtensions
{
    public static Task ExecuteAsync(
        this IResilienceService service,
        IResiliencePolicyProvider policyProvider,
        string policyName,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (policyProvider is null)
        {
            throw new ArgumentNullException(nameof(policyProvider));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var options = policyProvider.TryGetPolicy(policyName, out var resolved)
            ? resolved
            : null;

        return service.ExecuteAsync(action, options, ct);
    }

    public static Task<T> ExecuteAsync<T>(
        this IResilienceService service,
        IResiliencePolicyProvider policyProvider,
        string policyName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (policyProvider is null)
        {
            throw new ArgumentNullException(nameof(policyProvider));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var options = policyProvider.TryGetPolicy(policyName, out var resolved)
            ? resolved
            : null;

        return service.ExecuteAsync(action, options, ct);
    }
}
