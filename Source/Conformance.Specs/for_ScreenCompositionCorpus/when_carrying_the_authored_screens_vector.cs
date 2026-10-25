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
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
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
public partial class when_carrying_the_authored_screens_vector : given.the_planned_screen_composition_corpus
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

    // Raw Scene carries advisory selection hints; canonical creation intent can override these during rendering without changing these bytes.
    [Fact] void should_carry_the_advisory_collection_selection_hints() => Encoding.UTF8.GetString(_bytes).Split("\"rowAction\":true").Length.ShouldEqual(4);
    [Fact] void should_pin_the_authored_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("d186ca30122ac199487caac46223c862fbe1ac468a883fe5b2dcc6eef3c3bcbb");
    [Fact] void should_pin_the_authored_scene_length() => _bytes.Length.ShouldEqual(24023);
    [Fact] void should_carry_the_guarded_close_action() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"alternatives\"");
    [Fact] void should_carry_the_guarded_double_click_branches() => Encoding.UTF8.GetString(_bytes).ShouldContain($"\"path\":\"{SceneGuards.GuardPath}\"");
    [Fact] void should_type_the_close_guard_by_the_enumeration_of_the_details_status() => Guards().Count(_ => _.Contains("\"left\":{\"subject\":\"item.status\"},\"operator\":\"Equal\",\"right\":\"open\"", StringComparison.Ordinal) && _.Contains("\"enumeration\":[\"open\",\"closed\"]", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_type_every_status_comparison() => OpenStatusComparison.Count(Encoding.UTF8.GetString(_bytes)).ShouldEqual(ClosedEnumeration.Count(Encoding.UTF8.GetString(_bytes)));
    [Fact] void should_name_enumerations_as_the_scene_runtime_declares_them() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"trigger\":{\"kind\":\"DoubleClick\"}");
    [Fact] void should_not_camel_case_an_enumeration_name() => Encoding.UTF8.GetString(_bytes).ShouldNotContain("\"kind\":\"doubleClick\"");
    [Fact] void should_plan_the_authored_scene_with_its_guards() => _refusal.ShouldBeNull();
    [Fact] void should_plan_without_warnings_or_errors() => _plan!.Diagnostics.Where(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_reassemble_the_canonical_scene_byte_identically() => _plan!.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Bytes.ShouldEqual(_bytes);
    // Re-frozen for Cratis/Stage#707: the default scaffold is React Aria; only the PrimeReact removal and the Scene 4.80.0 / Components 4.34.0 pins change.
    // Re-frozen for #437: only the Matching package pin and host reference are added.
    // Re-frozen for the train4 review fixes: proven creations retain their identifier, gain createsEntity/x-creates and lose rowAction.
    [Fact] void should_confine_changes_to_common_and_using_placement() => QualifiedCommonOutput.Digest(_model, _plan!, new(ApplicationName, ApplicationName)).ShouldEqual("462a204306b8f2f2121741c00fdc03a3298c3aae0c1b28dca37a2003d351664c");
    [Fact] void should_pin_the_partitioned_artifact_digest() => FrozenScaffoldPins.Digest(_plan!).ShouldEqual("d46f79d1a92acb6c36f63f89e6bc27b2c92a23bd95bc58805a9f6e4ce1838c9e");
    [Fact] void should_pin_the_screen_fragments() => string.Join('\n', _plan!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}")).ShouldEqual("""
        Workspaces/Tracking/WorkItemComments/Screens.scene.json acd7f05ae339201f3867044a5a7ae3e7f3501e0ae90aac9b9742fa24cc0b98da 2270
        Workspaces/Tracking/WorkItemDetails/Screens.scene.json 69068d395013f46e2d1b6a40a47ba8e3d0dec57ff968b213d7b85bddde3c02d3 9827
        Workspaces/Tracking/WorkItemList/Screens.scene.json e83275bead9558ec7617fc163048fb1e5bd22bcea574e51784e83d2a93560389 6678
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

    IEnumerable<string> Guards() => GuardComparison.Matches(Encoding.UTF8.GetString(_bytes)).Select(_ => _.Value);

    [GeneratedRegex("\"subject\":\"item\\.status\"\\}[^}]*\"right\":\"open\"", RegexOptions.None, 1000)]
    private static partial Regex OpenStatusComparison { get; }

    [GeneratedRegex("\"enumeration\":\\[\"open\",\"closed\"\\]", RegexOptions.None, 1000)]
    private static partial Regex ClosedEnumeration { get; }

    [GeneratedRegex("\\{\"kind\":\"comparison\"[^{}]*\\{[^{}]*\\}[^{}]*\\}|\\{\"[^{}]*\"kind\":\"comparison\"[^{}]*\\{[^{}]*\\}[^{}]*\\}", RegexOptions.None, 1000)]
    private static partial Regex GuardComparison { get; }
}
