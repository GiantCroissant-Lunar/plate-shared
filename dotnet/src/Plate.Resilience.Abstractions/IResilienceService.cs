using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plate.Resilience;

public interface IResilienceService
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        ResiliencePolicyOptions? options = null,
        CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResiliencePolicyOptions? options = null,
        CancellationToken ct = default);
}
