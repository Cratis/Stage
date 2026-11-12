// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography;

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The grain that lists stored inbound requests. The open list is complete, because the dispatcher lives off it; the recent list keeps the newest
/// <see cref="Retained"/> identities, as it is a view for people, not the record.
/// </summary>
/// <param name="state">The persisted state.</param>
public sealed class InboundIndexGrain([PersistentState("inbound-index", WellKnownGrainStorageProviders.Workflows)] IPersistentState<InboundIndexState> state) : Grain, IInboundIndexGrain
{
    /// <summary>
    /// Gets how many identities the recent list keeps.
    /// </summary>
    public const int Retained = 5000;

    /// <inheritdoc/>
    public async Task Open(string id)
    {
        var changed = false;
        if (!state.State.Open.Contains(id))
        {
            state.State.Open.Add(id);
            changed = true;
        }

        if (!state.State.Recent.Contains(id))
        {
            state.State.Recent.Add(id);
            if (state.State.Recent.Count > Retained) state.State.Recent = [.. state.State.Recent.Skip(state.State.Recent.Count - Retained)];
            changed = true;
        }

        if (changed) await state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Close(string id)
    {
        if (state.State.Open.Remove(id)) await state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListOpen() => Task.FromResult<IReadOnlyList<string>>([.. state.State.Open]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> Recent(int limit) => Task.FromResult<IReadOnlyList<string>>([.. state.State.Recent.Reverse().Take(limit)]);
}

/// <summary>
/// The persisted state of an <see cref="InboundIndexGrain"/>.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Extensions.Inbound.InboundIndexState")]
public sealed class InboundIndexState
{
    /// <summary>
    /// Gets or sets the identities that wait for an attempt.
    /// </summary>
    [Id(0)]
    public IList<string> Open { get; set; } = [];

    /// <summary>
    /// Gets or sets the identities, oldest first.
    /// </summary>
    [Id(1)]
    public IList<string> Recent { get; set; } = [];
}
