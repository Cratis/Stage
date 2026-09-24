// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Executes selected semantic specifications in isolated memory. Arc's scenario replaces the process-wide
/// Internals.ServiceProvider and leaves it pointing at a disposed provider; run this executor in its own
/// process until Arc offers a scoped alternative. Serializing executor calls does not isolate a host.
/// </summary>
public interface ISemanticSpecificationExecutor
{
    /// <summary>
    /// Runs specifications in source order, refusing unsupported behavior before any effects.
    /// </summary>
    /// <param name="plan">The compiled semantic plan.</param>
    /// <param name="selection">The containment scopes to run.</param>
    /// <param name="options">The execution context.</param>
    /// <param name="cancellationToken">The cancellation signal.</param>
    /// <returns>A versioned execution report.</returns>
    Task<SemanticSpecificationRunReport> Run(SemanticExecutionPlan plan, SemanticSpecificationSelection selection, SemanticSpecificationRunOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies deterministic identity allocation when a future capability admits implicit destinations.
/// </summary>
public interface ISemanticIdentityAllocator
{
    /// <summary>
    /// Allocates an identity for a command and specification.
    /// </summary>
    /// <param name="command">The command identity.</param>
    /// <param name="specification">The specification identity.</param>
    /// <returns>The allocated value.</returns>
    SemanticValue Allocate(SemanticId command, SemanticId specification);
}

/// <summary>
/// Selects application, module, feature, slice or specification identities.
/// </summary>
/// <param name="Scopes">The selected identities; empty selects all specifications.</param>
public sealed record SemanticSpecificationSelection(ImmutableArray<SemanticId> Scopes)
{
    /// <summary>
    /// Selects all specifications.
    /// </summary>
    public static SemanticSpecificationSelection All { get; } = new([]);
}

/// <summary>
/// Provides execution context independent of process time and ambient identity.
/// </summary>
public sealed record SemanticSpecificationRunOptions
{
    /// <summary>
    /// Gets the clock used for event occurrence times.
    /// </summary>
    public TimeProvider Clock { get; init; } = new FixedSemanticClock();

    /// <summary>
    /// Reserved for future identity allocation. Implicit destinations are currently Unsupported(IdentityAllocation).
    /// </summary>
    public ISemanticIdentityAllocator Identities { get; init; } = new DeterministicSemanticIdentityAllocator();

    /// <summary>
    /// Gets the caller supplied to execution.
    /// </summary>
    public SemanticCaller? DefaultCaller { get; init; }

    /// <summary>
    /// Reserved for future tenant-scoped execution. The in-memory runner does not apply tenant isolation.
    /// </summary>
    public string Tenant { get; init; } = "default";
}

sealed class FixedSemanticClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
}

sealed class DeterministicSemanticIdentityAllocator : ISemanticIdentityAllocator
{
    public SemanticValue Allocate(SemanticId command, SemanticId specification) => SemanticValue.Text($"{command}:{specification}");
}
