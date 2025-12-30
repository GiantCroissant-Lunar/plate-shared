using System;
using System.Collections.Concurrent;

namespace Plate.Resilience;

public sealed class ResiliencePolicyRegistry : IResiliencePolicyProvider
{
    private readonly ConcurrentDictionary<string, ResiliencePolicyOptions> _policies = new(StringComparer.Ordinal);

    public bool TryGetPolicy(string policyName, out ResiliencePolicyOptions options)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            options = default!;
            return false;
        }

        return _policies.TryGetValue(policyName, out options!);
    }

    public void SetPolicy(string policyName, ResiliencePolicyOptions options)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(policyName));
        }

        _policies[policyName] = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool RemovePolicy(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return false;
        }

        return _policies.TryRemove(policyName, out _);
    }
}
