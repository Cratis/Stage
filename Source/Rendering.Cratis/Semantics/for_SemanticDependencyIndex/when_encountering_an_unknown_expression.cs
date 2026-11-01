// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_encountering_an_unknown_expression : given.a_dependency_index
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        var produced = command.Produces.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with { Produces = [produced with { Mappings = [produced.Mappings[0] with { Source = new future_expression() }] }] }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_not_claim_completeness() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_explain_the_unknown_expression() => _index.IncompleteReasons.Any(reason => reason.Contains("unknown variant", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_enum_discriminator() => SemanticDependencyVariants.Known((SemanticExpressionRootKind)int.MaxValue).ShouldBeFalse();
    [Fact] void should_refuse_an_unknown_reference_shape() => SemanticDependencyVariants.Known(new SemanticTypeReference((SemanticTypeReferenceKind)int.MaxValue, SemanticPrimitiveType.Unknown, default, false, false)).ShouldBeFalse();

    sealed record future_expression() : SemanticExpression(SemanticExpressionKind.Unknown);
}
