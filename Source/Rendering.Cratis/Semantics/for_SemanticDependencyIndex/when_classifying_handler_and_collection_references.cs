// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_classifying_handler_and_collection_references : Specification
{
    [Fact] void should_classify_the_command_handler() => Assert.Equal(SemanticDependencyKind.Expression, SemanticDependencySurface.Classify("SemanticCommand.Handler")?.Kind);
    [Fact] void should_classify_the_possible_events() => Assert.Equal(SemanticDependencyKind.ProducedEvent, SemanticDependencySurface.Classify("SemanticCommandHandler.PossibleEvents")?.Kind);
    [Fact] void should_classify_the_collection_property() => Assert.Equal(SemanticDependencyKind.Expression, SemanticDependencySurface.Classify("SemanticOneOfExpression.CollectionProperty")?.Kind);
}
