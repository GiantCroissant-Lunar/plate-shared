using System;

namespace Plate.Resilience;

public static class ResiliencePolicyProviderExtensions
{
    public static ResiliencePolicyOptions? TryGetPolicyOrNull(
        this IResiliencePolicyProvider provider,
        string? policyName)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(policyName))
        {
            return null;
        }

        return provider.TryGetPolicy(policyName, out var options) ? options : null;
    }
}
