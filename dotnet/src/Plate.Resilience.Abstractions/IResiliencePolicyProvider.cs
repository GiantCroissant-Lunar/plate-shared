using System;

namespace Plate.Resilience;

public interface IResiliencePolicyProvider
{
    bool TryGetPolicy(string policyName, out ResiliencePolicyOptions options);
}
