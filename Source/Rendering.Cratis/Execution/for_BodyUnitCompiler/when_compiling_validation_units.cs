// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_compiling_validation_units : Specification
{
    [Fact]
    void should_compile_the_real_rendered_wrapper_and_reuse_its_verified_pe()
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        var directory = Path.Combine(AppContext.BaseDirectory, "body-unit-cache", Guid.NewGuid().ToString("N"));
        var emissions = 0;
        var manifest = BodyUnitCompiler.Compile(loaded, directory, () => emissions++);
        var entry = manifest.Entries.Single();
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        BodyUnitCache.Verify(directory, entry).ShouldBeTrue();
        emissions.ShouldEqual(1);
        var cached = BodyUnitCompiler.Compile(loaded, directory, () => emissions++).Entries.Single();
        emissions.ShouldEqual(1);
        cached.PeSha256.ShouldEqual(entry.PeSha256);
        cached.UnitHash.ShouldEqual(entry.UnitHash);
        var corrupt = BodyUnitCache.PePath(directory, entry.UnitHash);
        File.WriteAllText(corrupt, "not an assembly");
        BodyUnitCache.Verify(directory, entry).ShouldBeFalse();
        BodyUnitCompiler.Compile(loaded, directory, () => emissions++).Entries.Single().PeSha256.ShouldEqual(entry.PeSha256);
        emissions.ShouldEqual(2);
    }

    [Fact]
    void should_not_widen_admission_when_chronicle_is_available_for_emission()
    {
        const string body = "return Cratis.Chronicle.Events.EventSourceId.New() != null;";
        var loaded = validation_body_model.Load(validation_body_model.Command(body));
        var context = BodyUnitCompiler.CreateContext(loaded);
        var requirement = loaded.ImplementationRequirements.Single();
        var verdict = Semantics.Validation.PureValidationAdmission.Analyze(body, context, loaded.TypedContextDescriptors.Single(), requirement);
        verdict.Accepted.ShouldBeFalse();
        var emissions = 0;
        var entry = BodyUnitCompiler.Compile(loaded, AppContext.BaseDirectory, () => emissions++).Entries.Single();
        entry.Admitted.ShouldBeFalse();
        entry.DiagnosticCode.ShouldEqual(verdict.Code);
        emissions.ShouldEqual(0);
    }

    [Theory]
    [InlineData("envelope", "STAGE-ESM-019")]
    [InlineData("language", "STAGE-ESM-019")]
    [InlineData("binding", "STAGE-ESM-020")]
    [InlineData("missing-context", "STAGE-ESM-021")]
    [InlineData("duplicate-context", "STAGE-ESM-021")]
    [InlineData("unsupported-kind", "STAGE-IMPL-003")]
    void should_refuse_invalid_units_without_emitting(string invalid, string code)
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        var requirement = loaded.ImplementationRequirements.Single();
        loaded = InvalidUnit(loaded, requirement, invalid);
        var emissions = 0;
        var entry = BodyUnitCompiler.Compile(loaded, AppContext.BaseDirectory, () => emissions++).Entries.Single();
        entry.Admitted.ShouldBeFalse();
        entry.DiagnosticCode.ShouldEqual(code);
        emissions.ShouldEqual(0);
    }

    static LoadedSemanticModel InvalidUnit(LoadedSemanticModel loaded, SemanticImplementationRequirement requirement, string invalid) =>
        invalid switch
        {
            "missing-context" => loaded with { TypedContextDescriptors = [] },
            "duplicate-context" => DuplicateContext(loaded),
            _ => loaded with { ImplementationRequirements = [InvalidRequirement(requirement, invalid)] }
        };

    static SemanticImplementationRequirement InvalidRequirement(SemanticImplementationRequirement requirement, string invalid) =>
        invalid switch
        {
            "envelope" => requirement with { ContextVersion = 2 },
            "language" => requirement with { Language = "javascript" },
            "binding" => requirement with { Source = null! },
            "unsupported-kind" => requirement with { Role = SemanticImplementationRole.CommandHandler },
            _ => throw new ArgumentException("Unknown invalid-unit fixture.", nameof(invalid))
        };

    static LoadedSemanticModel DuplicateContext(LoadedSemanticModel loaded) =>
        loaded with { TypedContextDescriptors = loaded.TypedContextDescriptors.AddRange(loaded.TypedContextDescriptors) };

    [Fact]
    void should_refuse_tampered_body_content_before_compilation()
    {
        var loaded = validation_body_model.Load(validation_body_model.Concept("return context.Value > 0;"));
        var requirement = loaded.ImplementationRequirements.Single();
        var manifest = BodyUnitCompiler.Compile(loaded with { ImplementationContents = loaded.ImplementationContents.SetItem(requirement.RequirementId, "return true;") }, AppContext.BaseDirectory);
        manifest.Entries.Single().Admitted.ShouldBeFalse();
        manifest.Entries.Single().DiagnosticCode.ShouldEqual("STAGE-ESM-056");
    }
}
