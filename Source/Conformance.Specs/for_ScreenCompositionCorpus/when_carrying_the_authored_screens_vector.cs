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

    [Fact] void should_pin_the_authored_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("ba09e56187e6ac5b2f05d145461b650242496a3381d68f8d40758fc69924cd73");
    [Fact] void should_pin_the_authored_scene_length() => _bytes.Length.ShouldEqual(23916);
    [Fact] void should_carry_the_guarded_close_action() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"alternatives\"");
    [Fact] void should_carry_the_guarded_double_click_branches() => Encoding.UTF8.GetString(_bytes).ShouldContain($"\"path\":\"{SceneGuards.GuardPath}\"");
    [Fact] void should_type_the_close_guard_by_the_enumeration_of_the_details_status() => Guards().Count(_ => _.Contains("\"left\":{\"subject\":\"item.status\"},\"operator\":\"Equal\",\"right\":\"open\"", StringComparison.Ordinal) && _.Contains("\"enumeration\":[\"open\",\"closed\"]", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_type_every_status_comparison() => OpenStatusComparison.Count(Encoding.UTF8.GetString(_bytes)).ShouldEqual(ClosedEnumeration.Count(Encoding.UTF8.GetString(_bytes)));
    [Fact] void should_name_enumerations_as_the_scene_runtime_declares_them() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"trigger\":{\"kind\":\"DoubleClick\"}");
    [Fact] void should_not_camel_case_an_enumeration_name() => Encoding.UTF8.GetString(_bytes).ShouldNotContain("\"kind\":\"doubleClick\"");
    [Fact] void should_plan_the_authored_scene_with_its_guards() => _refusal.ShouldBeNull();
    [Fact] void should_plan_without_warnings_or_errors() => _plan!.Diagnostics.Where(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_reassemble_the_canonical_scene_byte_identically() => _plan!.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Bytes.ShouldEqual(_bytes);

    // Both digests retain #494's authenticated endpoints, default spec callers, access README and inference diagnostics.
    // Main adds route-qualified query registrations, native query proxies, keyed screen identifiers and cleaned frontend tsconfigs.
    [Fact] void should_confine_changes_to_common_and_using_placement() => QualifiedCommonOutput.Digest(_model, _plan!, new(ApplicationName, ApplicationName)).ShouldEqual("216a05a3be0e7dbe2ac9f20f5e846ca97c2b64d69e617fc972e8b655acc60756");
    [Fact] void should_pin_the_partitioned_artifact_digest() => FrozenScaffoldPins.Digest(_plan!).ShouldEqual("55fc2e56681ef0a5733448e638f1c46a52853559cb36a06a0aa13f7fc80035aa");
    [Fact] void should_pin_the_screen_fragments() => string.Join('\n', _plan!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}")).ShouldEqual("""
        Workspaces/Tracking/WorkItemComments/Screens.scene.json acd7f05ae339201f3867044a5a7ae3e7f3501e0ae90aac9b9742fa24cc0b98da 2270
        Workspaces/Tracking/WorkItemDetails/Screens.scene.json 10054bae285b11e3bc411abf7f1231f8745fd5eeff694db5c80b8f830a6e5e5d 9765
        Workspaces/Tracking/WorkItemList/Screens.scene.json 997c6c77c83d9108a4a541d5e0be6e6189fc18ea3899184d0b1d5ef983b0ffb4 6633
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
