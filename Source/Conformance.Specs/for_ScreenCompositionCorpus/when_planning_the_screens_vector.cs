// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// Pins the complete Stage plan of the canonical screens vector - every artifact path in order, with its hash and
/// byte count - for every source form of the vector (Cratis/Screenplay#168).
/// </summary>
/// <remarks>
/// A Scene digest alone says the composition is stable; it says nothing about the backend, the scaffold or the
/// binding module a caller publishes next to it. The corpus pins no Stage artifact manifest of its own, so Stage
/// pins one here. A change to any planned byte has to show up as a change to this file.
/// </remarks>
public class when_planning_the_screens_vector : given.the_planned_screen_composition_corpus
{
    // Includes #494's authenticated endpoints, default spec callers, access README and inference diagnostics.
    // Main adds route-qualified query registrations, native query proxies and cleaned frontend tsconfigs.
    const string Digest = "143ba2405c9e50d4fe98e253716533830c356517e60c77c54744de3c77ed5ccf";

    const string QualifiedManifest = """
        .editorconfig 9e7d03793f5503ba48e324c287915ca597ab7d4c68b808c48202ea3c6a6a6c69 689
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx f44650b0ac67159ae951e1ee00f76268e10c0311a5d128d602591ecee727d1fc 2693
        .frontend/tsconfig.json b9167d1994863900fb2d66de275f33c37580839d752e802765b429d0bfb95469 982
        .frontend/tsconfig.node.json 3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342 290
        .frontend/vite-env.d.ts 65996936fbb042915f7b74a200fcdde7e410f32a669b1ab9597cfaa4b0faddb5 38
        .frontend/vite.config.ts 2563e2d7099206e3e6109995b94c2f532b6fee1b2b87041f74a8ac3b0b5654b1 1362
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        Common/CommentId.cs 71ae1b6c2a3591bfab1150eada7e73ec12fcc07647cb92c57d6198de07338e52 726
        Common/WorkItemId.cs c41a7000be4fdcd29954dffe48d67ecc2d84751f470850c3e49f750916ffa126 733
        Common/WorkStatus.cs 5bee5282aed332a1c26d9b125b73756d5a5e558d3ff3ced2a4ef0d01cac63978 307
        Directory.Build.props fb4abdf22956b96063dbf033d57b65bb965ad5679faeccbd0cbcd8947e7c0f99 1115
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props 969b29a5bdea717d8f06b5f39a9728ff47a958a8fc8f13a2ab21c169d7e43710 1098
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs e5010616b42fcfa3ebea105bd4f76a12423291f8b29f6ebb0035963b59b160f7 996
        README.md 29e57ed851e352d644de09dad0002e626150aea9741b6e1465003d9de4f75e1c 2682
        Workspaces.Specs/Workspaces.Specs.csproj b88f81e8d9f2f47b00113df7ec92e37919c703083d9b767a24451a5104d8f617 1105
        Workspaces.csproj b684abdda8f2c797e1725992283941a71cb92e51783c1bce75e0419b97758fff 1226
        Workspaces.slnx 650516e95954eebf4339f821450c06276740585167423ff70481565a1a03dda5 124
        Workspaces/Tracking/AddComment/AddComment.cs 5bcfbe0d84bf63d50873a9e9f22a0b4d7031fe188ea4cfd36ef03e081f6de387 1110
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs d285f7c6e5774aed8440bd4fcb72c60ef1b05614935b652fa5e0f91d943a14b2 3037
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 48e42f096a17c576c657056e1c63364958c549a0c8a745d810f421e755ce5b96 1077
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 179bc2f27597660d34842dd04e43ba99fc5b52e1c1c6a464716c4d174c755b0c 2807
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 683b93a7d2e75ab50d2871f33c0a96a440a276afe478afd43677fdff51595e75 1130
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs d92ca00a61a54fc984ba8c8ece368f3477f4794ea3cdf1d4031dcb95ee662707 2980
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 3020b773de8738b4889e9d18833e9ea98b4189e0d6479084cdeedf5174373354 1028
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs 650d8051a4c908c7933328e8d45f2d21aff8ab25d2f2e4d8a3c59185fd17152f 2859
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 6192a34d0521cdbb8e5b64b1a2bf5517755954235eb3589fdeda4f420f456e95 2174
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs adb3fd415af6b119c3a2c8a2961c3423bf8b50b71c9b934b52b6cb4a79d6be10 2740
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs a710e5bc8efadc457243626c45fa3be0fc2ab83920ff805d87a5e1bff7454981 2915
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 1866dab7e0acfd7cdacf8f834b2800a6ab1756357a9bd4f7c022de0ed923754c 2728
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 7ea80b334c19a341b800e564669df17d690bbab731c3e8bc4b039ff0141cdb0e 3199
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs 85c389d9b510cfbb130f4937d6468023e0331db217f6a91b9e6c8980916110c7 2493
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml 15878b8a6b7a4ca7f5888f2c9c380ad66d9c133514cba9a264f5520a52c32558 259
        package.json b1e03f04b82f520dc8e726bd42eb72bc30a072790c860ebb67d70c0200393ef8 1469
        scene.json 036c725d120d35592e7077d6d3bb2953413ba9c3bddb645d103e0dd962e0c97c 3971
        src/bindings.ts 26c5414114643e1a95d6cae0ac550193813053efead2fc81624791a290a001f7 2173
        src/stage-scene.json 036c725d120d35592e7077d6d3bb2953413ba9c3bddb645d103e0dd962e0c97c 3971
        src/stage.ts 3cb9a0d8518ac80881390dcb5706a82c50a3e75706653e4762735586c0249ebe 1081
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

    static readonly string Manifest = QualifiedManifest
        .Replace(".editorconfig 9e7d03793f5503ba48e324c287915ca597ab7d4c68b808c48202ea3c6a6a6c69 689", ".editorconfig 4cf0d1389cef373e18920c4fddb10948553e3c171badb2de66399717f870df76 741", StringComparison.Ordinal)
        .Replace("Common/CommentId.cs 71ae1b6c2a3591bfab1150eada7e73ec12fcc07647cb92c57d6198de07338e52 726", "Common/CommentId.cs 090339b5965d73ed60cb7e6b62bd8bfb9addfdc778f49ecb129d294a63c0b119 662", StringComparison.Ordinal)
        .Replace("Common/WorkItemId.cs c41a7000be4fdcd29954dffe48d67ecc2d84751f470850c3e49f750916ffa126 733", "Common/WorkItemId.cs 59ca162d48ce6b922cdf7176b53d85e5c5bac0e61b65da86d5f03858bb103cef 669", StringComparison.Ordinal);

    public static TheoryData<string> Forms => [.. ScreenCompositionCorpus.V1.SourceForms.Select(form => form.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_confine_changes_to_common_files_and_using_placement(string form)
    {
        var source = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == form);
        var planned = Plan(source.Documents, source.IdentityCatalogBytes.AsSpan(), carryScene: false);
        var restored = QualifiedCommonOutput.Restore(planned.Model, planned.Plan, new(ApplicationName, ApplicationName));
        var actual = restored.Select(FrozenScaffoldPins.Normalize).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ToArray();
        var expected = QualifiedManifest.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
        Assert.True(actual.SequenceEqual(expected), string.Join('\n', actual.Except(expected)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_without_diagnostics(string form) => PlanOf(form).Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_exactly_the_pinned_ordered_artifacts(string form)
    {
        var actual = PlanOf(form).Artifacts.Select(FrozenScaffoldPins.Normalize).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ToArray();
        Assert.True(actual.SequenceEqual(Pinned()), string.Join(Environment.NewLine, actual.Except(Pinned())));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_pinned_digest(string form) => FrozenScaffoldPins.Digest(PlanOf(form)).ShouldEqual(Digest);

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_change_only_the_runtime_pins_slice_sources_stage_import_and_runtime_copy_from_the_previous_frozen_plan(string form)
    {
        // Rebaselined for the @cratis/stage.runtime package (#499): beyond the original invariant, the previous plan's vendored
        // .frontend/stage/* runtime and styledPrimeReact.ts are no longer emitted and the entry point, vite config and package.json import/depend on the package. The
        // shell also declares Vite's client types in .frontend/vite-env.d.ts, which the vendored runtime used to carry.
        // The restored plan retains #494's authenticated endpoints, spec callers, access README and inference diagnostics,
        // plus main's route-qualified query registrations, native query proxies and cleaned frontend tsconfigs.
        var sourceForm = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == form);
        var planned = Plan(sourceForm.Documents, sourceForm.IdentityCatalogBytes.AsSpan(), carryScene: false);
        var plan = planned.Plan;
        ArtifactRenderPlan.ComputeDigest(
            plan.Target,

            // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
            "22.87.0",
            plan.Renderer,
            plan.RendererVersion,
            plan.ApplicationName,
            [.. QualifiedCommonOutput.Restore(planned.Model, plan, new(ApplicationName, ApplicationName)).Where(artifact => !artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => artifact.Kind == PlannedArtifactKind.Text
                ? PlannedArtifact.CreateText(
                    PreviousNames(artifact.RelativePath),
                    PreviousNames(PreviousPins(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))),
                    previous_slice_sources.Of(artifact, planned.Model))
                : artifact)],
            plan.Diagnostics).ShouldEqual("fbe0746d398b8204fcf50ded12b71e0c1c05781e95df5645ee483085fd87cc5e");
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_same_bytes_as_the_folder_form(string form) =>
        PlanOf(form).Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())).ShouldEqual(PlanOf("folder").Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())));

    [Fact] void should_cover_every_source_form() => Forms.Count.ShouldEqual(4);

    static string PreviousPins(string relativePath, string text) =>
        FrozenScaffoldPins.Normalize(relativePath, PreviousFrontendEntryPoint.Restore(relativePath, text), previous: true);

    // The previous frozen plan predates single-letter word splitting in snake-cased names (Cratis/Stage#417), which
    // rendered "AWorkItem" as "awork_item".
    static string PreviousNames(string text) => text
        .Replace("_a_work_item", "_awork_item", StringComparison.Ordinal)
        .Replace("adding_a_comment", "adding_acomment", StringComparison.Ordinal);

    static string[] Pinned() => [.. Manifest.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())];

    static ArtifactRenderPlan PlanOf(string name)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == name);
        return Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: false).Plan;
    }
}
