// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    ImmutableArray<PlannedArtifact> _qualified = [];

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

            // The previous pins predate resolved imports (#424), so they are compared with the fully qualified spelling.
            _qualified = QualifiedCommonOutput.Restore(_plan, () => Plan(form.Documents, catalog.AsSpan(), carryScene: true).Plan);
        });
    }

    [Fact] void should_pin_the_authored_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("a09710e28afae20990adf6477a6f4e95c99073a44abe56b0d2378bdb28231c10");
    [Fact] void should_pin_the_authored_scene_length() => _bytes.Length.ShouldEqual(23972);
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
    // Re-frozen for Cratis/Stage#816 and Cratis/Stage#757: only src/bindings.ts command imports and the package.json fundamentals resolution change.
    // Re-frozen for #741: only .gitignore gains the .yarn/ line.
    [Fact] void should_confine_changes_to_common_and_using_placement() => FrozenScaffoldPins.Digest(_plan!, Legacy(_qualified)).ShouldEqual("83c179eb6abdf5f34c1649bbb2065e446375bb0607b55754c0884aeafd213389");

    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives above the namespace.
    // Re-frozen for Cratis/Stage#816 and Cratis/Stage#757: only src/bindings.ts command imports and the package.json fundamentals resolution change.
    // Re-captured on Train 8b: only #660 adds CratisReadProtection On relative to main; all Scene artifacts match main.
    // Re-frozen for #741: only .gitignore gains the .yarn/ line.
    [Fact] void should_pin_the_partitioned_artifact_digest() => FrozenScaffoldPins.Digest(_plan!).ShouldEqual("2da4b0a9d5b3ce40ad34cba37bc9a28e43ef96721fc7823085e5f441ec3be63d");
    [Fact] void should_pin_the_screen_fragments() => string.Join('\n', _plan!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}")).ShouldEqual("""
        Workspaces/Tracking/WorkItemComments/Screens.scene.json acd7f05ae339201f3867044a5a7ae3e7f3501e0ae90aac9b9742fa24cc0b98da 2270
        Workspaces/Tracking/WorkItemDetails/Screens.scene.json 5c5a07d337d3f9410aa4bc506563d1f580c0149004da8bfcb8f131a14ede9a53 9793
        Workspaces/Tracking/WorkItemList/Screens.scene.json ebb54a4bc9dc126e98bb1ff94dd6452824637224c57939c33e1215edae93b4e3 6661
        """);

    static IEnumerable<PlannedArtifact> Legacy(IEnumerable<PlannedArtifact> artifacts) => SeparateSpecificationsLayout.RestoreWithLicenseHeader(artifacts, ApplicationName, ApplicationName)
        .Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                LegacyReadProtection.Restore(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                artifact.Sources)
            : artifact);

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
