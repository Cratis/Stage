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
    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives above the namespace.
    // Re-frozen for Cratis/Stage#816 (aliased Scene command imports in src/bindings.ts) and Cratis/Stage#757 (fundamentals resolution in package.json).
    // Re-frozen for Cratis/Stage#873
    const string Digest = "88e730afcba65ddf7fb0027b7478fa083d3304bd383fb0eff3818a5967d91146";

    // Re-frozen for Cratis/Stage#873
    const string Manifest = """
        .editorconfig 0071ecdc7a762e5966d5f6e6c72cc6b53d96c918c2e32dbe74059ffbc792859f 741
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx 257ace67a590a8f34d1dd3ee6cf9dded7a5ed824cca27347bcd8ebd771e7ed4b 2107
        .frontend/tsconfig.json b9167d1994863900fb2d66de275f33c37580839d752e802765b429d0bfb95469 982
        .frontend/tsconfig.node.json 3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342 290
        .frontend/vite-env.d.ts 65996936fbb042915f7b74a200fcdde7e410f32a669b1ab9597cfaa4b0faddb5 38
        .frontend/vite.config.ts 5daf7f13caaa4595c83ba8909b745687c932893405b4941bd55a4b16f1ee0361 1113
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        .yarnrc.yml 473e6def86fc03638120e0c01d0c8bbab095677256460fa4ea763e5d4697f270 25
        Common/CommentId.cs 57e703a97b83c6715cf6bbe082a6987bfecde236fa550cd589f38d4ab1cfda73 382
        Common/WorkItemId.cs 10002af59d1071f61a44e7d32c6b04abe03ec6cb7cbb96a9c0b65a5844c5bd5e 389
        Common/WorkStatus.cs 3e1702646d8b71cdd756e8280b50d50c922eb850c41c99b394da684f85ed95b3 158
        Directory.Build.props fb4abdf22956b96063dbf033d57b65bb965ad5679faeccbd0cbcd8947e7c0f99 1115
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props e35974ccfcc53743171f971cbcb7a90f524b8f9caa95bfc23f031b854c819458 1176
        GeneratedPolicyRegistration.cs 7962c40dc5f71ab971cf01ae185b06e584594e03161c3db0fed18363ca166100 593
        GlobalUsings.cs 74b2438fda9d5bc192e6fcd6b44837b6129b532faa99555d77013f62f1ec3f8e 137
        Program.cs f148d2954e15bdf675f63ed1472c70b1f6396db6a826f7644c4cedad3ca5dd56 847
        README.md 6afd097c34ac79a7e53e5c8da59c3d37d1146fc9703d14cf098fb47af0774e4f 1849
        Workspaces.csproj 93f351d3db49bcdc4dbc03efab9b636a6820f3e724aa6ed7ae944c0e31c7db2b 2364
        Workspaces.slnx f74d3ddcde0097a4bdb2ce376128f76d0ed516c309d01f273f9b92c394ba2166 62
        Workspaces/Tracking/AddComment/AddComment.cs 61d1be7adb16c8720965320ec9d5f0a57957a5e2bcfff37557c4343d03a77edb 513
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs 7fde109bec5d420e1abc797e0fae200abd0b1e46d85edd6cd976743d9ca4eef5 1442
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs b43f6729024fb4a0a7bcf3993e70db54dd9292d14815c908ec59ef551224fedc 477
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 517855c688172ce7542747386a1035b3e3c0f2606b5f9661a2c48435a44eb966 1309
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 70471d7eddb22194d44c42e69f81cbfd3b70571ce6b69efcb8b5af13dffcb9e0 529
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 4a0cc504bb25cf0a05d7cea13c5a849dd67063b677c7af9a646d96a0a337ccc9 1394
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 5d7080b6fdd423c625d5b3c122dcf20c6bae565982e63b5b55ebcb37340b39e6 479
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs 5fa0a9440faf0fa2a32922c3b2e253ff04b0ae697fbde3944240d7aebc759dc4 1381
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs aab31b30fd1319fec7f8874b45da27d31d29e6374cc6d14740a1abb81287ffba 1363
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs 24cb3f8a2d89bfe8210cfdde60761cc1837f7255a06ee57e7edf70b9e66128d7 1985
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs f694a593f3d9ab31da4e9aac65caedd6bc93cd6412e1c1320dbbf53a6bf463ac 2059
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs a1d3b07ad309cd796f9862a567ac6a5c8127c60cc5083d520c17322f94a716e4 1690
        Workspaces/Tracking/WorkItemList/WorkItemList.cs e3de173ced957343633e25af2dbf9127e5b376f34723ec7ae363676d129d56c5 2202
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs d8b0763763dd34ab7dc7433ffc16d6597e3987ef673c412616ff92c7fcc172da 1521
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml a92e4b00e441d43ea07066f158322c0d02a7e1cc1f2c5853b3547b21567fba4a 559
        package.json 66eb49c0984c0fa1afaea06b248293dd66d3488dd735a66ca41353d24f17a070 1255
        scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/bindings.ts 03e22a6c7526dfaeb680dbb58a79f3e49399c615debcfd8e2eafa86e8e6150bb 2325
        src/stage-scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/stage.ts 3cb9a0d8518ac80881390dcb5706a82c50a3e75706653e4762735586c0249ebe 1081
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

    // Re-frozen for Cratis/Stage#873
    const string QualifiedManifest = """
        .editorconfig 9e7d03793f5503ba48e324c287915ca597ab7d4c68b808c48202ea3c6a6a6c69 689
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx 257ace67a590a8f34d1dd3ee6cf9dded7a5ed824cca27347bcd8ebd771e7ed4b 2107
        .frontend/tsconfig.json b9167d1994863900fb2d66de275f33c37580839d752e802765b429d0bfb95469 982
        .frontend/tsconfig.node.json 3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342 290
        .frontend/vite-env.d.ts 65996936fbb042915f7b74a200fcdde7e410f32a669b1ab9597cfaa4b0faddb5 38
        .frontend/vite.config.ts 5daf7f13caaa4595c83ba8909b745687c932893405b4941bd55a4b16f1ee0361 1113
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        .yarnrc.yml 473e6def86fc03638120e0c01d0c8bbab095677256460fa4ea763e5d4697f270 25
        Common/CommentId.cs 71ae1b6c2a3591bfab1150eada7e73ec12fcc07647cb92c57d6198de07338e52 726
        Common/WorkItemId.cs c41a7000be4fdcd29954dffe48d67ecc2d84751f470850c3e49f750916ffa126 733
        Common/WorkStatus.cs 5bee5282aed332a1c26d9b125b73756d5a5e558d3ff3ced2a4ef0d01cac63978 307
        Directory.Build.props fb4abdf22956b96063dbf033d57b65bb965ad5679faeccbd0cbcd8947e7c0f99 1115
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props e35974ccfcc53743171f971cbcb7a90f524b8f9caa95bfc23f031b854c819458 1176
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs e5010616b42fcfa3ebea105bd4f76a12423291f8b29f6ebb0035963b59b160f7 996
        README.md 07ae5cc82d0a7f0367ef58ebe49ebe9082757b29aee950e8993a76f84e3b7869 1789
        Workspaces.Specs/Workspaces.Specs.csproj b88f81e8d9f2f47b00113df7ec92e37919c703083d9b767a24451a5104d8f617 1105
        Workspaces.csproj 41af7d8fcdebd9454b740eea141dcdb1c15594e1719b132628ba87dfa440f2d4 1288
        Workspaces.slnx 650516e95954eebf4339f821450c06276740585167423ff70481565a1a03dda5 124
        Workspaces/Tracking/AddComment/AddComment.cs e6b1cd4db8918ab4eb9421f51e0c1224213252929dbfe3460a917dea88431c59 1115
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs d6a95f25c93d76ddee8c6b645dd40a42168cd797b7cd63168e7782c9d7613ac9 2508
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 336c9b53311c6bddde7371b4e2e3018e70aa2b60acf87e614aa9867362d4afd8 1082
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 7f1c87b85363001ca03696cb584e04167797de3ced67236dbe012665606b6e2a 2278
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs dcee13484087c939041d761f8a1853d88f9a4100d86b9452cd4eb5d6a65fe4f2 1135
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 3f4cbf67bf3fa0eaaaa023ac1e60a81ee2b9e4e9502f96ce943de5e10c1c4f3a 2451
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 2bbf45806cd32a070025b607f825f01fa2c60d3ffb70c66baf3313e5602f100b 1033
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs 979ae38ea8a44bc9553a2100737b17b9ddc3c9e045448c2b30799710b4ead99e 2330
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 65ca5ed242aea42e3c4a06697e4695e414534b1f1e7ae8c6fe544720efb8fa2f 2179
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs adb3fd415af6b119c3a2c8a2961c3423bf8b50b71c9b934b52b6cb4a79d6be10 2740
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs b20d80c6e2c6ecd7f1ca5fe79745386c2608bddb1acb47ea5c7399bb4905268d 2920
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 1866dab7e0acfd7cdacf8f834b2800a6ab1756357a9bd4f7c022de0ed923754c 2728
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 75aca71a66d0cec11f78c95dba6d241af52ea49576a4e51826cb03c40242676e 3209
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs 85c389d9b510cfbb130f4937d6468023e0331db217f6a91b9e6c8980916110c7 2493
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml a92e4b00e441d43ea07066f158322c0d02a7e1cc1f2c5853b3547b21567fba4a 559
        package.json 66eb49c0984c0fa1afaea06b248293dd66d3488dd735a66ca41353d24f17a070 1255
        scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/bindings.ts 03e22a6c7526dfaeb680dbb58a79f3e49399c615debcfd8e2eafa86e8e6150bb 2325
        src/stage-scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/stage.ts 3cb9a0d8518ac80881390dcb5706a82c50a3e75706653e4762735586c0249ebe 1081
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

    public static TheoryData<string> Forms => [.. ScreenCompositionCorpus.V1.SourceForms.Select(form => form.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_confine_changes_to_common_files_and_using_placement(string form)
    {
        var source = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == form);
        var planned = Plan(source.Documents, source.IdentityCatalogBytes.AsSpan(), carryScene: false);
        var restored = Legacy(QualifiedCommonOutput.Restore(planned.Plan, () => PlanOf(form)));
        var actual = restored.Select(FrozenScaffoldPins.Normalize).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ToArray();
        var expected = QualifiedManifest.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
        Assert.True(actual.SequenceEqual(expected), string.Join('\n', actual.Except(expected)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_without_diagnostics(string form) => PlanOf(form).Diagnostics.ShouldBeEmpty();

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_exactly_the_pinned_ordered_artifacts(string form)
    {
        var actual = PlanOf(form).Artifacts.Select(FrozenScaffoldPins.Normalize).Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ToArray();
        Assert.True(actual.SequenceEqual(Pinned(Manifest)), string.Join(Environment.NewLine, actual.Except(Pinned(Manifest))));
    }

    // The intermediate pins from before #424, when Common files carried globally rooted imports inside the namespace,
    // are gone: that spelling no longer exists. should_confine_changes_to_common_files_and_using_placement proves the
    // stronger property, that the fully qualified spelling still reproduces the earlier frozen manifest byte for byte.
    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_pinned_digest(string form) => FrozenScaffoldPins.Digest(PlanOf(form)).ShouldEqual(Digest);

    // Re-frozen for Cratis/Stage#873
    [Theory]
    [MemberData(nameof(Forms))]
    public void should_change_only_the_runtime_pins_slice_sources_stage_import_and_runtime_copy_from_the_previous_frozen_plan(string form)
    {
        // Rebaselined for the @cratis/stage.runtime package (#499): beyond the original invariant, the previous plan's vendored
        // .frontend/stage/* runtime and styledPrimeReact.ts are no longer emitted and the entry point, vite config and package.json import/depend on the package. The
        // shell also declares Vite's client types in .frontend/vite-env.d.ts, which the vendored runtime used to carry.

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
            [.. Legacy(QualifiedCommonOutput.Restore(plan, () => PlanOf(form))).Where(artifact => !artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => artifact.Kind == PlannedArtifactKind.Text
                ? PlannedArtifact.CreateText(
                    PreviousNames(artifact.RelativePath),
                    PreviousNames(PreviousPins(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))),
                    previous_slice_sources.Of(artifact, planned.Model))
                : artifact)],
            plan.Diagnostics).ShouldEqual("677c0b187fb051a4bf07147932066b899da5e347658d75fa3ba128d3f7eca13d");
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_same_bytes_as_the_folder_form(string form) =>
        PlanOf(form).Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())).ShouldEqual(PlanOf("folder").Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())));

    [Fact] void should_cover_every_source_form() => Forms.Count.ShouldEqual(4);

    // Cratis/Stage#816 and Cratis/Stage#757 are the only changes to src/bindings.ts and package.json since the previous plan was frozen.
    static string PreviousPins(string relativePath, string text) =>
        PreviousSceneBindingsAndResolutions.Restore(relativePath, FrozenScaffoldPins.Normalize(relativePath, PreviousFrontendEntryPoint.Restore(relativePath, text), previous: true));

    // The previous frozen plan predates single-letter word splitting in snake-cased names (Cratis/Stage#417), which
    // rendered "AWorkItem" as "awork_item".
    static string PreviousNames(string text) => text
        .Replace("_a_work_item", "_awork_item", StringComparison.Ordinal)
        .Replace("adding_a_comment", "adding_acomment", StringComparison.Ordinal);

    static string[] Pinned(string manifest) => [.. manifest.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())];

    static IEnumerable<PlannedArtifact> Legacy(IEnumerable<PlannedArtifact> artifacts) => SeparateSpecificationsLayout.RestoreWithLicenseHeader(artifacts, ApplicationName, ApplicationName);

    static ArtifactRenderPlan PlanOf(string name)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == name);
        return Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: false).Plan;
    }
}
