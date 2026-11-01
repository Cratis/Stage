// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class golden_vector_refusals
{
    internal static void AssertNamedRefusals(int version)
    {
        var golden = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(version).Bytes.AsSpan());
        var model = ExecutableSemanticModel.Create(golden.LanguageVersion, golden.SemanticVersion, golden.Application with { Name = "Golden" });
        var profile = CratisRendering.CreateProfile("Golden", new("Golden", "Golden"));
        var plan = new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model, []).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        var label = model.Application.Concepts.Single(concept => concept.Name == "Label");
        var slices = SemanticVersionFeatures.Slices(model.Application).ToArray();
        var creation = slices.Single(slice => slice.Name == "Creation");
        var create = creation.Commands.Single(command => command.Name == "CreateEntity");
        var validate = slices.SelectMany(slice => slice.Commands).Single(command => command.Name == "ValidateEntity");
        var rule = label.Validations[0].Name ?? label.Validations[0].Kind.ToString();

        Assert.Equal($"Concept 'Label' combines enumerated values with validation '{rule}'; Stage renders enumerated values as a C# enum, which carries no concept validator, so these rules would be lost (Cratis/Stage#514).",
            Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-002" && diagnostic.Artifact == label.Id)).Message);
        Assert.Equal("Constraint 'UniqueEntityCode' cannot preserve changes to multiple claims or releases in one command 'CreateEntity' batch (EntityCreated, EntityMaybeSelected).",
            Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-014" && diagnostic.Artifact == create.Id)).Message);
        string[] expectedConstraints =
        [
            "Constraint 'UniqueEntityCode' targets composite properties (Label, Code) on 'EntityCreated'; Chronicle's string hash can collide with distinct Screenplay values.",
            "Constraint 'UniqueEntityCode' targets nontext or collection property 'Id' on 'EntityMaybeSelected'; Chronicle's string hash can collide with distinct Screenplay values.",
            "Constraint 'UniqueEntityCode' targets optional properties (Id) on 'EntityMaybeSelected'; Chronicle #4122 indexes null as an empty-string claim."
        ];
        Assert.Equal(expectedConstraints, plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-014" && diagnostic.Artifact == creation.Id).Select(diagnostic => diagnostic.Message));
        Assert.Equal("Authorized command 'ValidateEntity' declares no identifier; it needs exactly one identifier to preserve the reference subject.",
            Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Artifact == validate.Id)).Message);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }
}
