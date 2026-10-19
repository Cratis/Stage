// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_generated_values : Specification
{
    SemanticCommand _command = null!;
    SemanticConcept _concept = null!;

    void Establish()
    {
        var model = invoice_model.Compile("concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        returns id");
        _command = SemanticVersionFeatures.Slices(model.Application).Single().Commands.Single();
        _concept = model.Application.Concepts.Single();
    }

    [Fact] void should_admit_uuid_concepts() => SemanticVersionFeatures.UnsupportedGenerated(_command, [_concept]).ShouldBeEmpty();
    [Fact] void should_refuse_raw_uuids() => Refusals(_command.Properties.Single().Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.Uuid }).ShouldContainOnly(SemanticVersionFeatures.Generated);
    [Fact] void should_refuse_optional_generation() => Refusals(_command.Properties.Single().Type with { IsOptional = true }).ShouldContainOnly(SemanticVersionFeatures.Generated);
    [Fact] void should_refuse_collection_generation() => Refusals(_command.Properties.Single().Type with { IsCollection = true }).ShouldContainOnly(SemanticVersionFeatures.Generated);
    [Fact] void should_refuse_non_uuid_concepts() => SemanticVersionFeatures.UnsupportedGenerated(_command, [_concept with { Primitive = SemanticPrimitiveType.Text }]).Select(feature => feature.Code).ShouldContainOnly(SemanticVersionFeatures.Generated);
    [Fact] void should_admit_error_rules_on_generated_concepts() => SemanticVersionFeatures.UnsupportedGenerated(_command, [_concept with { Validations = [new(_concept.Id, SemanticValidationRuleKind.NotEmpty, null, null)] }]).ShouldBeEmpty();
    [Fact] void should_admit_a_factory_type_name_collision() => SemanticVersionFeatures.UnsupportedGenerated(_command, [_concept with { Name = "New" }]).ShouldBeEmpty();

    [Fact]
    void should_admit_generated_occurrence_local_name_collisions()
    {
        var model = invoice_model.Compile("concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        occurred Id generated\n        produces event Created\n          timestamp DateTime = $context.occurred\n        returns id");
        var command = SemanticVersionFeatures.Slices(model.Application).Single().Commands.Single();
        SemanticVersionFeatures.InCommand(command).ShouldBeEmpty();
    }

    IEnumerable<string> Refusals(SemanticTypeReference type) => SemanticVersionFeatures.UnsupportedGenerated(_command with { Properties = [_command.Properties.Single() with { Type = type }] }, [_concept]).Select(feature => feature.Code);
}
