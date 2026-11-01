// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyDiagnostics;

public class when_a_body_kind_is_not_executed : Specification
{
    [Theory]
    [InlineData(SemanticImplementationRole.RulePredicate, "S1")]
    [InlineData(SemanticImplementationRole.CommandValidation, "S2")]
    [InlineData(SemanticImplementationRole.ConceptValidation, "S2")]
    [InlineData(SemanticImplementationRole.ReducerTransition, "S3")]
    [InlineData(SemanticImplementationRole.PolicyPredicate, "S4")]
    [InlineData(SemanticImplementationRole.CommandHandler, "S5")]
    [InlineData(SemanticImplementationRole.ReactionEffect, "S6")]
    [InlineData(SemanticImplementationRole.QueryPerformer, "S7")]
    void should_name_the_kind_and_its_slice(SemanticImplementationRole role, string slice) =>
        BodyDiagnostics.NotExecuted(role).ShouldEqual($"{role} is not executed by `cratis run` yet (Stage#891 slice {slice}).");
}
