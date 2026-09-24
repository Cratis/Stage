// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Exposes the semantic execution surface only when a host registers an admitted runtime.
/// </summary>
public interface ISemanticRuntime
{
    /// <summary>
    /// Gets the admitted execution plan.
    /// </summary>
    SemanticExecutionPlan Plan { get; }

    /// <summary>
    /// Evaluates a command and commits its facts after a successful append, unless validation only was requested.
    /// </summary>
    /// <param name="command">The command to evaluate.</param>
    /// <param name="payload">The JSON command values.</param>
    /// <param name="principal">The current caller.</param>
    /// <param name="occurrence">The occurrence metadata.</param>
    /// <param name="validateOnly">Whether the command is a dry run.</param>
    /// <returns>The semantic outcome.</returns>
    Task<SemanticExecutionResult> Execute(SemanticCommand command, IReadOnlyDictionary<string, JsonElement> payload, ClaimsPrincipal principal, SemanticCommandOccurrence occurrence, bool validateOnly);

    /// <summary>
    /// Executes one keyed query against the committed world.
    /// </summary>
    /// <param name="query">The admitted query.</param>
    /// <param name="key">The typed lookup key.</param>
    /// <param name="principal">The caller.</param>
    /// <returns>The semantic outcome.</returns>
    Task<SemanticExecutionResult> Query(SemanticKeyedQuery query, SemanticValue key, ClaimsPrincipal principal);

    /// <summary>
    /// Reads the committed instances of one read model.
    /// </summary>
    /// <param name="model">The read model identity.</param>
    /// <returns>The committed instances.</returns>
    Task<ImmutableArray<SemanticReadModelInstance>> ReadModels(SemanticId model);
}
