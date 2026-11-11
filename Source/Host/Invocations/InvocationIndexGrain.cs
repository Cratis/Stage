// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The grain that lists invocations for the Workbench. It keeps the newest <see cref="Retained"/> identities: the list is a view for people, not the record.
/// </summary>
/// <param name="state">The persisted state.</param>
public sealed class InvocationIndexGrain([PersistentState("invocation-index", WellKnownGrainStorageProviders.Workflows)] IPersistentState<InvocationIndexState> state) : Grain, IInvocationIndexGrain
{
    /// <summary>
    /// Gets how many identities the list keeps.
    /// </summary>
    public const int Retained = 5000;

    /// <inheritdoc/>
    public async Task Add(string identity)
    {
        if (state.State.Identities.Contains(identity)) return;
        state.State.Identities.Add(identity);
        if (state.State.Identities.Count > Retained) state.State.Identities = [.. state.State.Identities.Skip(state.State.Identities.Count - Retained)];
        await state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> List(int limit) =>
        Task.FromResult<IReadOnlyList<string>>([.. state.State.Identities.Reverse().Take(limit)]);
}

/// <summary>
/// The persisted state of an <see cref="InvocationIndexGrain"/>.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationIndexState")]
public sealed class InvocationIndexState
{
    /// <summary>
    /// Gets or sets the identities, oldest first.
    /// </summary>
    [Id(0)]
    public IList<string> Identities { get; set; } = [];
}
