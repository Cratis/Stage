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
    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives above the namespace.
    // Re-frozen for Cratis/Stage#816 (aliased Scene command imports in src/bindings.ts) and Cratis/Stage#757 (fundamentals resolution in package.json).
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    const string Digest = "93109e3427f6759a26cf5cbf8cec7ae8d1905fd72eb797dea3722723c156eca6";

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
        Directory.Build.props b074ad4117a0d8e81466865fbb69d5d57a9c9cb7c0d3886d2575c1009322597a 1167
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props e35974ccfcc53743171f971cbcb7a90f524b8f9caa95bfc23f031b854c819458 1176
        GeneratedPolicyRegistration.cs 7962c40dc5f71ab971cf01ae185b06e584594e03161c3db0fed18363ca166100 593
        GlobalUsings.cs 74b2438fda9d5bc192e6fcd6b44837b6129b532faa99555d77013f62f1ec3f8e 137
        Program.cs f148d2954e15bdf675f63ed1472c70b1f6396db6a826f7644c4cedad3ca5dd56 847
        README.md 1a542107c560b1b710966f45da3e3c57e80c73613ff1f6860648be93a2e3f825 3329
        Workspaces.csproj 93f351d3db49bcdc4dbc03efab9b636a6820f3e724aa6ed7ae944c0e31c7db2b 2364
        Workspaces.slnx f74d3ddcde0097a4bdb2ce376128f76d0ed516c309d01f273f9b92c394ba2166 62
        Workspaces/Tracking/AddComment/AddComment.cs a0ca676b35a4adb886c4a57f6c4177da82cbd6a6d5371e75095ff6969e6c50c1 508
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs 4dc12fd9d939e72379a9085a60120153812a9cdb9f23ae0b9b243409f4f570f1 1905
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs aeda6c010da0eaec88c0279b961819a58b39b4606231707fe78a5b7d1af476bc 472
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 02a4a8d28a9f84c0c7df74df7af0f30c94f6d738dafe55972f885c3eb459820f 1772
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs f03949fc4650c87e58bc259e4e8afac5286d6db0827cc4bde8587a76c3cc223b 524
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 163c5d4b82d2fd9cc38091fbf8d1b656f0028a37f75035f83a034b5e5ea18143 1857
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 97a9f31b7b9c2a99a05b7497531f863fb82c53c0224914ed6bb7b3895e61251c 474
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs d7e5f175745b71ee792fb0ea655a36af42bbe581784ac24677ac34fe451d4387 1844
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs d334adc17ef804064112f45e1ed4cbae5d9dc80a540b21f27a98041f9aee30ca 1358
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs 24cb3f8a2d89bfe8210cfdde60761cc1837f7255a06ee57e7edf70b9e66128d7 1985
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs 98ecea3fb8f292843c641b039ee98c909f750df11377fa6bf6c5eb834c49766e 2054
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs a1d3b07ad309cd796f9862a567ac6a5c8127c60cc5083d520c17322f94a716e4 1690
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 28963d784b33cce8bec3df105e65d196f437b1520d9c0f778bce89a7f83b0505 2192
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs d8b0763763dd34ab7dc7433ffc16d6597e3987ef673c412616ff92c7fcc172da 1521
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml 15878b8a6b7a4ca7f5888f2c9c380ad66d9c133514cba9a264f5520a52c32558 259
        package.json 66eb49c0984c0fa1afaea06b248293dd66d3488dd735a66ca41353d24f17a070 1255
        scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/bindings.ts 03e22a6c7526dfaeb680dbb58a79f3e49399c615debcfd8e2eafa86e8e6150bb 2325
        src/stage-scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/stage.ts 3cb9a0d8518ac80881390dcb5706a82c50a3e75706653e4762735586c0249ebe 1081
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

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
        README.md 58c6e18b3c7d9e5de69cfff4978a22c457299bf29f8f8e43fa9bf30b7499c31c 3269
        Workspaces.Specs/Workspaces.Specs.csproj b88f81e8d9f2f47b00113df7ec92e37919c703083d9b767a24451a5104d8f617 1105
        Workspaces.csproj 41af7d8fcdebd9454b740eea141dcdb1c15594e1719b132628ba87dfa440f2d4 1288
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
    public void should_plan_without_diagnostics(string form) => PlanOf(form).Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();

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
            plan.Diagnostics).ShouldEqual("b8fe5055383050ec563f87ee42d334c234e9b16d0d21c80b747cafe8b0e8d2e8");
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

    static IEnumerable<PlannedArtifact> Legacy(IEnumerable<PlannedArtifact> artifacts) => SeparateSpecificationsLayout.RestoreWithLicenseHeader(artifacts, ApplicationName, ApplicationName)
        .Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                LegacyReadProtection.Restore(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                artifact.Sources)
            : artifact);

    static ArtifactRenderPlan PlanOf(string name)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == name);
        return Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: false).Plan;
    }
}
