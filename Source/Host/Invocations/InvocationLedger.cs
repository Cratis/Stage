// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The <see cref="IInvocationLedger"/> over the invocation grains.
/// </summary>
/// <param name="grains">The grain factory of the silo.</param>
[IgnoreConvention]
public sealed class InvocationLedger(IGrainFactory grains) : IInvocationLedger
{
    /// <inheritdoc/>
    public async Task<InvocationRecord> Record(AbilityDescriptor descriptor, InvocationOrigin origin, string answerPolicy = "")
    {
        var identity = InvocationIdentity.For(origin, descriptor.Reference);
        var proposed = new InvocationRecord(identity, descriptor.Reference.Capability, descriptor.Effect, descriptor.PackageDigest, origin, InvocationStatus.Recorded, DateTimeOffset.UtcNow)
        {
            AnswerPolicy = answerPolicy
        };
        await grains.GetGrain<IInvocationIndexGrain>(IInvocationIndexGrain.Key).Add(identity);
        return await Of(identity).Record(proposed);
    }

    /// <inheritdoc/>
    public IInvocationGrain Of(string identity) => grains.GetGrain<IInvocationGrain>(identity);

    /// <inheritdoc/>
    public Task<InvocationRecord?> Get(string identity) => Of(identity).Get();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InvocationRecord>> List(InvocationStatus? status = null, int limit = 200)
    {
        var identities = await grains.GetGrain<IInvocationIndexGrain>(IInvocationIndexGrain.Key).List(InvocationIndexGrain.Retained);
        var records = new List<InvocationRecord>();
        foreach (var identity in identities)
        {
            if (await Get(identity) is not { } record || (status is not null && record.Status != status)) continue;
            records.Add(record);
            if (records.Count == limit) break;
        }

        return records;
    }
}
