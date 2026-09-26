// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

// Supplies the three indexes consumed by the shared scoped-projection admission source.
internal sealed class SemanticApplicationContext(SemanticExecutionPlan plan)
{
    public IReadOnlyDictionary<SemanticId, SemanticEventContract> Events => plan.Events;
    public IReadOnlyDictionary<SemanticId, SemanticCompositeType> Types { get; } = plan.Model.Application.Types.ToDictionary(type => type.Id);
    public IReadOnlyDictionary<SemanticId, SemanticConcept> Concepts { get; } = plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
}
