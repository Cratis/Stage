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
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for #741 (.gitignore gains .yarn/) and #734 (.yarnrc.yml preapproves @cratis/*).
    // Re-frozen when Debug specifications gained the Chronicle.Testing telemetry dependency.
    // Re-frozen for Scene 4.86.0.
    // Recomputed from train 9 execution after combining generated documentation, concepts and shared Common output.
    // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
    // Re-frozen for Cratis/Stage#937: only Directory.Packages.props pins Pkcs to 10.0.14 and documents Xml runtime servicing.
    const string Digest = "9d3431871c72a4de5bac801d5fbacfc9e41b8520a0de0e087f5a055e5ac5f1ce";
    // Re-frozen for Cratis/Stage#926: the scaffold hosts an Orleans silo (Program.cs, Directory.Packages.props, the project).
    // Re-frozen for Chronicle 19.114.0 and Cratis.Orleans 1.33.1: Program.cs no longer registers a telemetry callback.

    const string Manifest = """
        .editorconfig 0071ecdc7a762e5966d5f6e6c72cc6b53d96c918c2e32dbe74059ffbc792859f 741
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx 257ace67a590a8f34d1dd3ee6cf9dded7a5ed824cca27347bcd8ebd771e7ed4b 2107
        .frontend/tsconfig.json b9167d1994863900fb2d66de275f33c37580839d752e802765b429d0bfb95469 982
        .frontend/tsconfig.node.json 3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342 290
        .frontend/vite-env.d.ts 65996936fbb042915f7b74a200fcdde7e410f32a669b1ab9597cfaa4b0faddb5 38
        .frontend/vite.config.ts 5daf7f13caaa4595c83ba8909b745687c932893405b4941bd55a4b16f1ee0361 1113
        .gitignore cfff10c57387b861a42002754373868d7167021fa46ee762ec879c5cb0275b6a 260
        .yarnrc.yml 90be03827dbdc6c1875cdf7c57ea44b1eb5bc71e6692f46dab07dcec2a288b37 65
        Common/CommentId.cs b09abdd1ae5570070ca8c6c49323ff1077789fccdbeba5dcdeaf19a68af03103 857
        Common/WorkItemId.cs d23f662b4e209e76408cce40888ab0c9207d1b4d7814b9257bc199cde5e2bda5 869
        Common/WorkStatus.cs 39e8088132ed10030ab679cbd735db36c75cd2dfc5c9650596aa9c285458cce3 282
        Directory.Build.props b074ad4117a0d8e81466865fbb69d5d57a9c9cb7c0d3886d2575c1009322597a 1167
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props 81b89e942c8899995bae84cdfcda5a8d8a811f63761ca65759c8ffd6518b46d1 1837
        GeneratedAuthentication.cs ed09e251946fc352764a02a5b0ba94af3f465c824458b163fc4dc0fd958d7d29 5225
        GeneratedAuthenticationPerimeter.cs 81cd5b2cf77e385a07fcd4ff6d413be8e79436b1226242e9aa9ac461d6b6e960 9693
        GeneratedPolicyRegistration.cs 7962c40dc5f71ab971cf01ae185b06e584594e03161c3db0fed18363ca166100 593
        GlobalUsings.cs 74b2438fda9d5bc192e6fcd6b44837b6129b532faa99555d77013f62f1ec3f8e 137
        Program.cs d352c47ea54cc42361bbad4ff47754e1a5b8faa17cd30de29206febee13a908b 1621
        README.md aa64c1ef6846ab9796feb390c9aa112864126aaac54c9c81c0e50fea02d38e74 2392
        Workspaces.csproj 0d000d6abddab1f7509655d4280ab861d62795f668bcbfaf56f82619e4e93799 2611
        Workspaces.slnx f74d3ddcde0097a4bdb2ce376128f76d0ed516c309d01f273f9b92c394ba2166 62
        Workspaces/Tracking/AddComment/AddComment.cs 8ca9724697e5cbf55e8c5245c666a43d94e1a63eb322b36651e6d9500ce89c67 944
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs 5f692eef442134983246c572182e653f6775912ed3154134f4c62ddd45f9b56b 1525
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 61d4446ebddd0af9c79cdd5d266dcb420e1d70466af983ff293686fe8aa331ce 770
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 205c40e13b8f4f7d61dc471a2d5e8f5f1f105acf639d530cb0ab9c88bbeb9c1c 1395
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs d1ff5b12eaafe22990c09cbf64218f9f5b1c22985b92784326aed0dc826b5539 953
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 8392c71d00791fc10124c42d1c44bcf34635f66b4ffaff83d49c811afe98a5f9 1481
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs a9d87ce6caa9d2b2118e903f5c1499fde1d881a94edd00cae4f68bdd4b2bdfc1 815
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs 43091e7c853e227b4dca5bedc51cc1a024164a90cefba751890a65bbf2888470 1468
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 90c5a76ec9ba0b444f17811a6b4b518dac813f7f34396b758ca6d613e495526d 1854
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs 24cb3f8a2d89bfe8210cfdde60761cc1837f7255a06ee57e7edf70b9e66128d7 1985
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs 3089ef23281101382686daa8971fdf67d89beb24afd824881250e4cddd73e682 2547
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs a1d3b07ad309cd796f9862a567ac6a5c8127c60cc5083d520c17322f94a716e4 1690
        Workspaces/Tracking/WorkItemList/WorkItemList.cs af2715d313231d906a2868cd91fff091846f24626141e57b2c1744d3e83230f9 2906
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs d8b0763763dd34ab7dc7433ffc16d6597e3987ef673c412616ff92c7fcc172da 1521
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml a92e4b00e441d43ea07066f158322c0d02a7e1cc1f2c5853b3547b21567fba4a 559
        package.json 66eb49c0984c0fa1afaea06b248293dd66d3488dd735a66ca41353d24f17a070 1255
        scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/bindings.ts 03e22a6c7526dfaeb680dbb58a79f3e49399c615debcfd8e2eafa86e8e6150bb 2325
        src/stage-scene.json f295e8fd70f788d651c363b00357f0a678c2fbabcdd10039994fc1fa9697038b 4435
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
        .gitignore cfff10c57387b861a42002754373868d7167021fa46ee762ec879c5cb0275b6a 260
        .yarnrc.yml 90be03827dbdc6c1875cdf7c57ea44b1eb5bc71e6692f46dab07dcec2a288b37 65
        Common/CommentId.cs 2651823873f3fceb34c04b456f3ccf04a4bc5e76381a5f6e38e9893d6af88e5d 1201
        Common/WorkItemId.cs 6cccba7c19f37effa332a37b720ace84152adc9353f89a6b6e58d9b9815be380 1213
        Common/WorkStatus.cs 94c14a6093b208dfe5065c1320725638b94db7a004b164fef1cc4765e967f36a 431
        Directory.Build.props fb4abdf22956b96063dbf033d57b65bb965ad5679faeccbd0cbcd8947e7c0f99 1115
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props 81b89e942c8899995bae84cdfcda5a8d8a811f63761ca65759c8ffd6518b46d1 1837
        GeneratedAuthentication.cs ff86455b7f123bb85d611a5fabc40fd3e124ea6e6724ba9cca9d0b730e105ac2 5374
        GeneratedAuthenticationPerimeter.cs aef7e8ae1697056711fe37948080bbda478ba5bf26cbccdf8614c6578b96041b 9842
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs 028d29e6c973ddc29a574e42097884a196c6be8398bf0aecf2d7864ea5804813 1770
        README.md 30cac1be79d8dfeb616a606e0f54a3ddbc819d8641cef281c0d946c92ad647c4 2332
        Workspaces.Specs/Workspaces.Specs.csproj b88f81e8d9f2f47b00113df7ec92e37919c703083d9b767a24451a5104d8f617 1105
        Workspaces.csproj e216c063f5215c42c3bfe394edb60d886ad927ea0eb42db59c520ea3facf4d65 1456
        Workspaces.slnx 650516e95954eebf4339f821450c06276740585167423ff70481565a1a03dda5 124
        Workspaces/Tracking/AddComment/AddComment.cs 51ca54f7865f5b7f6b5e9a389cd862bdacfb1498ef2a55f1984712a86457253b 1546
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs a7d95a77837912a3396b6a7bacff2d303c8fd6c6e05247386395d80a1580289f 2495
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 56504a610b2631425abfc441d54abe14b4db4bd5629fd9b65df6a6f37e259c2c 1375
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 6f836120c90313f466ffc25527e41d21f769d6ac2e181dacc75349b674e06054 2240
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 74df6b7cb0351a33ff1f8f8c798b548bac44c545a163159530cfda8201ab06e1 1559
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 9900dd42b7c115af90cb2913dc2cb2cfb4ad4cdaa2282dc265e09f570281f857 2442
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 28fcd4158cf671444296f00d8fc554c9ea9f3fbebec26ba1fe82e18a316e0960 1369
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs ba679d4cfc2be46c6ba8d98430ac297c2be60b264cf5340f8b949267e639f455 2293
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs ce3a4c47e03d3e2989c503427d993214189ed7945a24155fdf0728639b2e117d 2670
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs adb3fd415af6b119c3a2c8a2961c3423bf8b50b71c9b934b52b6cb4a79d6be10 2740
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs ed0c18b8f25fc25669c259a557dfdce80c003a3b545fd4d9504d49f5fc551bc3 3408
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 1866dab7e0acfd7cdacf8f834b2800a6ab1756357a9bd4f7c022de0ed923754c 2728
        Workspaces/Tracking/WorkItemList/WorkItemList.cs e60af8c7b014748fcb338b83048fd52a157228e7234a8d61c6674f75594d2500 3913
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs 85c389d9b510cfbb130f4937d6468023e0331db217f6a91b9e6c8980916110c7 2493
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml a92e4b00e441d43ea07066f158322c0d02a7e1cc1f2c5853b3547b21567fba4a 559
        package.json 66eb49c0984c0fa1afaea06b248293dd66d3488dd735a66ca41353d24f17a070 1255
        scene.json ebb374ed2550f019cbd51055f7498fdf87e51db7a0d85479f7c65274fce7d55e 3894
        src/bindings.ts 03e22a6c7526dfaeb680dbb58a79f3e49399c615debcfd8e2eafa86e8e6150bb 2325
        src/stage-scene.json f295e8fd70f788d651c363b00357f0a678c2fbabcdd10039994fc1fa9697038b 4435
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
            plan.Diagnostics).ShouldEqual("2546db57627c2ebfd5cefd81eae47cff5c742fb8ca92b0081cd08fa40c41cf6b"); // Re-frozen for #741 (.gitignore gains the .yarn/ line), Scene 4.86.0, Cratis/Stage#120 merged with train 10 and #937 (only Directory.Packages.props pins Pkcs and documents Xml runtime servicing).
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
