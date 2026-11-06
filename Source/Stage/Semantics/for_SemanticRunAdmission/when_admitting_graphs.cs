// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Semantics.for_SemanticRunAdmission.given;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_admitting_graphs : Specification
{
    ExecutableSemanticModel _graphs = null!;
    ExecutableSemanticModel _clock = null!;
    ExecutableSemanticModel _plain = null!;

    void Establish()
    {
        _graphs = a_model_at_an_esm_pair.From(38, 0, application => application with
        {
            Graphs = [new("Organization", [new("Person", [new("personId", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), true)])], [])]
        });
        _clock = a_model_at_an_esm_pair.From(38, 0, application => application with { TimeZone = new("Europe/Oslo", true) });
        _plain = a_model_at_an_esm_pair.From(38, 0);
    }

    [Fact] void should_run_the_v38_pair() => SemanticRunAdmission.SupportsRun(_graphs).ShouldBeTrue();
    [Fact] void should_say_that_the_runtime_does_not_project_the_graph() => SemanticRunAdmission.ModelFeatures(_graphs).Single().Details.ShouldContain($"{SemanticRunAdmission.GraphsNotProjected}:");
    [Fact] void should_attribute_it_to_the_application() => SemanticRunAdmission.ModelFeatures(_graphs).Single().Artifact.ShouldEqual(_graphs.Application.Id.ToString());
    [Fact] void should_not_let_it_block_the_model() => SemanticRunAdmission.RunsAround(_graphs.Application, SemanticRunAdmission.ModelFeatures(_graphs).Single()).ShouldNotBeNull();
    [Fact] void should_not_block_the_model_with_it() => SemanticRunAdmission.BlockingModelFeatures(_graphs.Application).ShouldBeEmpty();
    [Fact] void should_say_nothing_about_a_model_without_graphs() => SemanticRunAdmission.ModelFeatures(_plain).ShouldBeEmpty();
    [Fact] void should_refuse_the_v37_clock_constructs_of_a_v38_model() => SemanticRunAdmission.ModelFeatures(_clock).ShouldContain(feature => feature.Details.Contains("STAGE-ESM-016", StringComparison.Ordinal));
}
