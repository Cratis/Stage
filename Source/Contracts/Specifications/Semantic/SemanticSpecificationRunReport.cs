// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications.Semantic;

/// <summary>
/// The outcome of executing a semantic specification.
/// </summary>
public enum SemanticSpecificationOutcome
{
    /// <summary>
    /// The expectations matched.
    /// </summary>
    Passed,

    /// <summary>
    /// Execution or comparison failed.
    /// </summary>
    Failed,

    /// <summary>
    /// A required capability is not available.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The run was interrupted.
    /// </summary>
    Cancelled
}

/// <summary>
/// Capabilities that may block an in-memory Stage execution before it starts.
/// </summary>
public enum StageExecutionCapability
{
    /// <summary>
    /// Command dispatch.
    /// </summary>
    Command,

    /// <summary>
    /// Query dispatch.
    /// </summary>
    Query,

    /// <summary>
    /// Identity allocation.
    /// </summary>
    IdentityAllocation,

    /// <summary>
    /// Read-model projection.
    /// </summary>
    Projection,

    /// <summary>
    /// Specification dispatch.
    /// </summary>
    Specification,

    /// <summary>
    /// Authorization.
    /// </summary>
    Authorization,

    /// <summary>
    /// Command requirements.
    /// </summary>
    Requirement,

    /// <summary>
    /// Append-time constraints.
    /// </summary>
    Constraint,

    /// <summary>
    /// Event occurrence context.
    /// </summary>
    Occurrence,

    /// <summary>
    /// Seeded read-model state.
    /// </summary>
    GivenReadModel,

    /// <summary>
    /// Reactions.
    /// </summary>
    Reaction,

    /// <summary>
    /// Time-sensitive behavior.
    /// </summary>
    Time,

    /// <summary>
    /// External effects.
    /// </summary>
    ExternalEffect,

    /// <summary>
    /// Blocked semantic plan.
    /// </summary>
    PlanIssue
}

/// <summary>
/// Describes a capability Stage cannot execute for a particular construct.
/// </summary>
/// <param name="Capability">The missing capability.</param>
/// <param name="Construct">The semantic identity of the offending construct, if available.</param>
/// <param name="Details">The reason execution was not admitted.</param>
public sealed record SemanticUnsupportedCapability(StageExecutionCapability Capability, string? Construct, string Details);

/// <summary>
/// A normalized fact in the execution trace, with canonical JSON values keyed by semantic property identity.
/// </summary>
/// <param name="EventContract">The event contract semantic identity.</param>
/// <param name="EventSourceType">The source identity type, when available.</param>
/// <param name="EventSource">The canonical JSON identity value.</param>
/// <param name="Values">The property values keyed by semantic identity.</param>
public sealed record SemanticTraceFact(string EventContract, string? EventSourceType, string EventSource, IReadOnlyDictionary<string, string> Values);

/// <summary>
/// The observable result of a semantic execution, independent of the execution host.
/// </summary>
/// <param name="Facts">The facts produced in append order.</param>
/// <param name="ReadModels">The normalized keyed read-model states.</param>
/// <param name="Queries">The normalized query results.</param>
/// <param name="Rejection">The rejection message, when rejected.</param>
public sealed record SemanticExecutionTrace(
    IReadOnlyList<SemanticTraceFact> Facts,
    IReadOnlyDictionary<string, string> ReadModels,
    IReadOnlyDictionary<string, string> Queries,
    string? Rejection);

/// <summary>
/// The result of executing one Screenplay specification.
/// </summary>
/// <param name="SpecificationId">The specification semantic identity.</param>
/// <param name="Name">The authored name.</param>
/// <param name="SliceId">The owning slice semantic identity.</param>
/// <param name="SliceKind">The owning slice kind.</param>
/// <param name="Outcome">The run outcome.</param>
/// <param name="ExecutionKind">Accepted, Rejected or Conflict when execution occurred.</param>
/// <param name="Unsupported">The blocking capability, if any.</param>
/// <param name="Failures">The expectation failures.</param>
/// <param name="Trace">The observed execution trace, if any.</param>
public sealed record SemanticSpecificationRunRecord(
    string SpecificationId,
    string Name,
    string SliceId,
    string SliceKind,
    SemanticSpecificationOutcome Outcome,
    string? ExecutionKind,
    SemanticUnsupportedCapability? Unsupported,
    IReadOnlyList<string> Failures,
    SemanticExecutionTrace? Trace);

/// <summary>
/// The versioned results of running a selection of semantic specifications.
/// </summary>
/// <param name="SchemaVersion">The results file schema version.</param>
/// <param name="ApplicationId">The application semantic identity.</param>
/// <param name="SemanticRevision">The executable model revision.</param>
/// <param name="Results">The individual results in deterministic order.</param>
public sealed record SemanticSpecificationRunReport(
    string SchemaVersion,
    string ApplicationId,
    string SemanticRevision,
    IReadOnlyList<SemanticSpecificationRunRecord> Results)
{
    /// <summary>
    /// Whether execution completed without cancellation.
    /// </summary>
    public bool Completed => Results.All(result => result.Outcome != SemanticSpecificationOutcome.Cancelled);
}
