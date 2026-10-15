// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
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
    const string Digest = "0f8dc52e3c77d034e0ce0f4c317bd699cf121a8484c0b17f911e87206ef05c3e";

    const string Manifest = """
        .editorconfig 9e7d03793f5503ba48e324c287915ca597ab7d4c68b808c48202ea3c6a6a6c69 689
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx e4dc56b56e3e3855a1fdeedf2c14c131db52853c73f87b3b28d51b30a0e361b7 2723
        .frontend/stage/App.tsx c3417b3461d13b094e7ed8f56ce4f0aba2fd264f854f80cbdd4bd51bfc058940 20099
        .frontend/stage/StageChrome.tsx 2cbcc9370c6e6385d097a4c2f96af89830371a1ce9efa3490b5058789cbfb925 7731
        .frontend/stage/StageCommandForm.tsx 07c4a176399590ee73a3ef5ce96d3389995f2f36204b8d08faa7903c061a2a81 22785
        .frontend/stage/StageGuardedAction.tsx 8c950711a7cd89ddd11191f846bf5230da93cf2a31a450976ff22d26e6210c5e 6921
        .frontend/stage/app.css b40e04c8475164eff7c08b96ddb498363e05f012493551a1c9296c02092098ea 3429
        .frontend/stage/blueprint.ts f1a7ee148e14a7c1f03af45cd6aa5f67d0c4ae89705df119bb6fa22819ec167e 7687
        .frontend/stage/navigationTree.ts b07b69a65ad734b534cc865355e210c938aa0832057483e4ccdfbd04545fdfc3 7290
        .frontend/stage/stageCommands.ts 48d56bfa7efb1ac14806a71ede7eba498e8ee29212ca159f81317b58621d9f41 2039
        .frontend/stage/stageComponents.tsx 0353eada936a391eea64b83f0097089090cb7fcf2bb2ad9f5d94b21d4d8aae3e 13750
        .frontend/stage/stageData.tsx d47040fee417d01d666e0bafb10a52e8c3bdc44ea06b05af2a7e8dfd9f06578d 18535
        .frontend/stage/stageDataSources.ts ade1a9a757ec28de294254bcb59ee95df1e03d047036e9281915665c0d19d888 4645
        .frontend/stage/stageGuards.ts d3caad28af719787f1007a8824898c86e6a4ac2eba863c36c4afaaae45b4cdf1 9330
        .frontend/stage/stageNavigation.ts 46fc13d64e68d906f47a2c4fb92ade4ed61535b3ec63ca40624d4fe2c9437633 2529
        .frontend/stage/stageNavigationModel.ts 69dd3672424cf10888c0ef8d37df2550224acafbadaf97a15bea19f7e877998c 2874
        .frontend/stage/stageRoutes.ts 7d85720c1d04fbf81da0bb6a287b67a58fa5ec6e275f04595250c16cc4b9a348 1792
        .frontend/stage/stageRowInteractions.ts 11d46c9b7e3dc182feb9629eb21d3d5b181b4119d3a03c4e565b814bfc2caf6f 3600
        .frontend/stage/stageSlices.ts 308e2d06c0571d98a13fecb4bac2549d2ccd2949e6bd8ffa1e71c7b308a30a31 3452
        .frontend/stage/stageSource.ts 0d8df29508315a5d17abe3bcf928bef4a1b05f1115395bfbe3356c3df7569817 4127
        .frontend/stage/stageTheme.ts 41c903960761179212feba27f3e3352666c54a6364b3b0a2223589e028df4398 1097
        .frontend/stage/useStrings.ts 7e9b26328e986b05f504ed40ae0656236d023b01ea4a1bf790220dd2f2309e17 2449
        .frontend/stage/vite-env.d.ts e24655f2bbc165e2dcbd988edb4a9a28f50fb3d2d9d78c775c7d0ee144bcce10 498
        .frontend/styledPrimeReact.ts 411262e641b07da4957bb52bf84deecdeef6319dbe64a1f1cba828065b402feb 3204
        .frontend/tsconfig.json efa4d0a8fdc813c520c240d2df0cd9211132fcde952f3d21f244c1a74ab124bf 1013
        .frontend/tsconfig.node.json 8d6d5e5ed11a91fdfa530d705f55c9676f62f12e72aa21b45502ecbadc27607b 313
        .frontend/vite.config.ts f4c1f53dda10136c81243564eee59796c43cbf8485479de9d24fdcb8431b79bc 1354
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        Common/CommentId.cs 71ae1b6c2a3591bfab1150eada7e73ec12fcc07647cb92c57d6198de07338e52 726
        Common/WorkItemId.cs c41a7000be4fdcd29954dffe48d67ecc2d84751f470850c3e49f750916ffa126 733
        Common/WorkStatus.cs 5bee5282aed332a1c26d9b125b73756d5a5e558d3ff3ced2a4ef0d01cac63978 307
        Directory.Build.props fb4abdf22956b96063dbf033d57b65bb965ad5679faeccbd0cbcd8947e7c0f99 1115
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props 3466b52e44e1e7dba425a78889ac7310cf4f3130d9fb4451f0ae1513ab1dff5c 1098
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs e5010616b42fcfa3ebea105bd4f76a12423291f8b29f6ebb0035963b59b160f7 996
        README.md 2bed375a9bda95aa35cf3d523c58d79efd60f865ccf42ba3abb5a1117b928288 1732
        Workspaces.Specs/Workspaces.Specs.csproj b88f81e8d9f2f47b00113df7ec92e37919c703083d9b767a24451a5104d8f617 1105
        Workspaces.csproj b684abdda8f2c797e1725992283941a71cb92e51783c1bce75e0419b97758fff 1226
        Workspaces.slnx 650516e95954eebf4339f821450c06276740585167423ff70481565a1a03dda5 124
        Workspaces/Tracking/AddComment/AddComment.cs e6b1cd4db8918ab4eb9421f51e0c1224213252929dbfe3460a917dea88431c59 1115
        Workspaces/Tracking/AddComment/when_adding_a_comment.cs c6a0a667972606327dc13ea098c197c3367e2816b64a3d10a7dc6ea11c3a9ab5 1886
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 336c9b53311c6bddde7371b4e2e3018e70aa2b60acf87e614aa9867362d4afd8 1082
        Workspaces/Tracking/CloseWorkItem/when_closing_a_work_item.cs 3df508cbb675a5c5f58d0c065e2466dbafc739b0f1b6ee6c41533f179bee05d8 2066
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 17c6b0173d5bb17eb3c5c084ec802f8df5206def555e1e584f629f8b473a58e1 1121
        Workspaces/Tracking/CreateWorkItem/when_creating_a_work_item.cs 4ae512b44b77f98267c41c296898b0ff82c27ada76190ff0f3cbc2f8e926515e 1757
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 2bbf45806cd32a070025b607f825f01fa2c60d3ffb70c66baf3313e5602f100b 1033
        Workspaces/Tracking/RenameWorkItem/when_renaming_a_work_item.cs b5352766f1427c7ccc81066d0359f15b3631241c561a82bfdbec0896c359d501 2086
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 65ca5ed242aea42e3c4a06697e4695e414534b1f1e7ae8c6fe544720efb8fa2f 2179
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_a_work_item_is_queried.cs a6284ec8b7b5e48a19bd9a1aae6f45b5bd7cbed86cfe5e449560ab65e64e0084 2304
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs b20d80c6e2c6ecd7f1ca5fe79745386c2608bddb1acb47ea5c7399bb4905268d 2920
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 7348d49d01490ecb86f5b8489b95721c792d565c24b46b3304b851c1d0e91107 2833
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 75aca71a66d0cec11f78c95dba6d241af52ea49576a4e51826cb03c40242676e 3209
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs f646a742678761c49e10be6d010dfe8f52bb2d0f615348dd724e202e122ed70d 2502
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml 100ae9158ee336bbf09c743961ed2614a289f94e3022a301204cf8b76ad8c09f 259
        package.json a1b2e660cd913705d805f3275b92970062335c71be383c4387ff69536b99effc 1431
        scene.json 61125b4580a21576f69fae0dc03e94ef9abea2302101e8fab9b937c7827ca57c 3918
        src/bindings.ts acd1c3406a5f7a8cdd640e3682001634290a1e3359c63dfe86494badec891833 1467
        src/stage-scene.json 61125b4580a21576f69fae0dc03e94ef9abea2302101e8fab9b937c7827ca57c 3918
        src/stage.ts d26df8127bbb827c594535e28c533e21be7cdf6794c34103da829f628069cb2a 1803
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

    public static TheoryData<string> Forms => [.. ScreenCompositionCorpus.V1.SourceForms.Select(form => form.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_without_diagnostics(string form) => PlanOf(form).Diagnostics.ShouldBeEmpty();

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_exactly_the_pinned_ordered_artifacts(string form)
    {
        var actual = PlanOf(form).Artifacts.Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ToArray();
        Assert.True(actual.SequenceEqual(Pinned()), string.Join(Environment.NewLine, actual.Except(Pinned())));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_pinned_digest(string form) => PlanOf(form).Digest.ShouldEqual(Digest);

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_change_only_the_runtime_pins_slice_sources_and_optional_stage_import_from_the_previous_frozen_plan(string form)
    {
        var sourceForm = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == form);
        var planned = Plan(sourceForm.Documents, sourceForm.IdentityCatalogBytes.AsSpan(), carryScene: false);
        var plan = planned.Plan;
        ArtifactRenderPlan.ComputeDigest(
            plan.Target,
            // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
            plan.TargetVersion.Replace("22.104.0", "22.87.0", StringComparison.Ordinal),
            plan.Renderer,
            plan.RendererVersion,
            plan.ApplicationName,
            [.. plan.Artifacts.Where(artifact => !artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => artifact.Kind == PlannedArtifactKind.Text
                ? PlannedArtifact.CreateText(
                    PreviousNames(artifact.RelativePath),
                    PreviousNames(PreviousPins(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))),
                    previous_slice_sources.Of(artifact, planned.Model))
                : artifact)],
            plan.Diagnostics).ShouldEqual("21b7c3659b5db3d49e21270732fb89071447ec4ff6accaa3f2f248d622a28cd4");
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_same_bytes_as_the_folder_form(string form) =>
        PlanOf(form).Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())).ShouldEqual(PlanOf("folder").Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())));

    [Fact] void should_cover_every_source_form() => Forms.Count.ShouldEqual(4);

    // The previous frozen plan predates the Chronicle 19.66.0 and backend Arc 22.104.0 scaffold pins. CPM now keeps
    // those pins in Directory.Packages.props; frontend files may already name 22.100.1 for unrelated packages.
    static string PreviousPins(string relativePath, string text)
    {
        var previous = PreviousFrontendEntryPoint.Restore(relativePath, text).Replace("19.66.0", "19.55.2", StringComparison.Ordinal).Replace("4.38.0", "4.22.1", StringComparison.Ordinal);

        return relativePath.EndsWith("Directory.Packages.props", StringComparison.Ordinal)
            ? previous.Replace("22.104.0", "22.87.0", StringComparison.Ordinal)
            : previous;
    }

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
