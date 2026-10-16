// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The screens vector's authored composition, translated by Stage and carried into a runnable plan.
/// </summary>
/// <remarks>
/// The authored Scene bytes are pinned. The vector's guarded Close action and guarded double click have guards
/// the Stage runtime evaluates, so they are carried into the runnable plan as authored. A guard it cannot
/// evaluate is still refused; that refusal is specified in Rendering.Cratis and Contracts.
/// </remarks>
public class when_carrying_the_authored_screens_vector : given.the_planned_screen_composition_corpus
{
    byte[] _bytes = null!;
    Exception? _refusal;
    ArtifactRenderPlan? _plan;
    ExecutableSemanticModel _model = null!;

    void Because()
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");
        _bytes = Encoding.UTF8.GetBytes(CanonicalSceneJson.Serialize(SceneOf(form.Documents)));
        var catalog = form.IdentityCatalogBytes;
        _refusal = Catch.Exception(() =>
        {
            var planned = Plan(form.Documents, catalog.AsSpan(), carryScene: true);
            _plan = planned.Plan;
            _model = planned.Model;
        });
    }

    [Fact] void should_pin_the_authored_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("25ccba40d3be0513ea07a07825d4f51c3a7c2187bdc653895209fc6711ccb3c1");
    [Fact] void should_pin_the_authored_scene_length() => _bytes.Length.ShouldEqual(23340);
    [Fact] void should_carry_the_guarded_close_action() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"alternatives\"");
    [Fact] void should_carry_the_guarded_double_click_branches() => Encoding.UTF8.GetString(_bytes).ShouldContain($"\"path\":\"{SceneGuards.GuardPath}\"");
    [Fact] void should_type_the_close_guard_by_the_enumeration_of_the_details_status() => Guards().Count(_ => _.Contains("\"left\":{\"subject\":\"item.status\"},\"operator\":\"Equal\",\"right\":\"open\"", StringComparison.Ordinal) && _.Contains("\"enumeration\":[\"open\",\"closed\"]", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_type_every_status_comparison() => Regex.Matches(Encoding.UTF8.GetString(_bytes), "\"subject\":\"item\\.status\"\\}[^}]*\"right\":\"open\"").Count.ShouldEqual(Regex.Matches(Encoding.UTF8.GetString(_bytes), "\"enumeration\":\\[\"open\",\"closed\"\\]").Count);
    [Fact] void should_name_enumerations_as_the_scene_runtime_declares_them() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"trigger\":{\"kind\":\"DoubleClick\"}");
    [Fact] void should_not_camel_case_an_enumeration_name() => Encoding.UTF8.GetString(_bytes).ShouldNotContain("\"kind\":\"doubleClick\"");
    [Fact] void should_plan_the_authored_scene_with_its_guards() => _refusal.ShouldBeNull();
    [Fact] void should_plan_without_warnings_or_errors() => _plan!.Diagnostics.Where(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_reassemble_the_canonical_scene_byte_identically() => _plan!.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Bytes.ShouldEqual(_bytes);
    [Fact] void should_confine_changes_to_common_and_using_placement() => QualifiedCommonOutput.Digest(_model, _plan!, new(ApplicationName, ApplicationName)).ShouldEqual("d1842d283a791bd8196434710e3cb37a97d13daaf265f7490821e5aa691d923a");
    [Fact] void should_pin_the_partitioned_artifact_digest() => _plan!.Digest.ShouldEqual("8de8e5c3d0fe8ff75fb345024889bdc533fb43745111ef568ccbf906a38ffcc0");
    [Fact] void should_pin_the_screen_fragments() => string.Join('\n', _plan!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}")).ShouldEqual("""
        Workspaces/Tracking/WorkItemComments/Screens.scene.json 24fb27aa838c752d1032fbe31fc49502ff2b444cc2453741bb8efb08506858be 2223
        Workspaces/Tracking/WorkItemDetails/Screens.scene.json 6748a60904bb7a192d0adcd630c4788ac299907c24400bf7e4a86a2e3b68f84d 9719
        Workspaces/Tracking/WorkItemList/Screens.scene.json 1eae9c370f2327e1bb5b10e94ac9895a555065c56d25caa30b1847fb3758fdbc 6183
        """);

    public static TheoryData<string> Forms => [.. ScreenCompositionCorpus.V1.SourceForms.Select(form => form.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_carry_identical_fragments_and_preserve_each_source_forms_scene(string name)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(form => form.Name == name);
        var plan = Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: true).Plan;
        Fragments(plan).ShouldEqual(Fragments(_plan!));
        plan.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Bytes.ShouldEqual(Encoding.UTF8.GetBytes(CanonicalSceneJson.Serialize(SceneOf(form.Documents))));
    }

    static IEnumerable<string> Fragments(ArtifactRenderPlan plan) => plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal))
        .Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {string.Join(',', artifact.Sources)}");

    IEnumerable<string> Guards() => Regex.Matches(Encoding.UTF8.GetString(_bytes), "\\{\"kind\":\"comparison\"[^{}]*\\{[^{}]*\\}[^{}]*\\}|\\{\"[^{}]*\"kind\":\"comparison\"[^{}]*\\{[^{}]*\\}[^{}]*\\}").Select(_ => _.Value);
}
