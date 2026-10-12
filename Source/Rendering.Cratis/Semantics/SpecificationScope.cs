// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The shared state while rendering one command specification.
/// </summary>
/// <param name="Builder">The builder receiving the generated source.</param>
/// <param name="Specification">The semantic specification being rendered.</param>
/// <param name="Command">The command the specification exercises.</param>
/// <param name="Context">The indexed semantic application.</param>
/// <param name="Types">The type system rendering types and values.</param>
/// <param name="SeedInLog">Whether the given events are seeded through the event log.</param>
/// <param name="CommandName">The rendered command type name.</param>
/// <param name="Arguments">The lazily rendered command constructor arguments.</param>
internal sealed record SpecificationScope(
    CSharpCodeBuilder Builder,
    SemanticSpecification Specification,
    SemanticCommand Command,
    SemanticApplicationContext Context,
    SemanticTypeSystem Types,
    bool SeedInLog,
    string CommandName,
    IEnumerable<string> Arguments)
{
    /// <summary>
    /// Gets a value indicating whether the number of given events is tracked.
    /// </summary>
    public bool CountsGivenEvents => SeedInLog || Specification.ThenDenied || !Specification.ThenErrors.IsEmpty;
}
