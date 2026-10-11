// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
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
    const string Digest = "06a6dd01b7a95aa08954fa3a6daa345995ee7773deaaec8d7eb3c083d3c1fdd3";

    const string Manifest = """
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx f1be33ab4c4b17540e101e671276f76ac33b0b9f79f1cd536f2f1cf5eb235d65 2253
        .frontend/stage/App.tsx 027b120bc72bc8e5dd74b6c3503e8cac5e86901bd600fde50f8e15ffef9f9aa3 20629
        .frontend/stage/FlowArrangementView.tsx 828b0518dd9d98a23605452355cb9a8a4e070bf98f61b6c014b0f705585cec93 2792
        .frontend/stage/StageChrome.tsx e13793cf12a2dbdbce7b59b0e3525d33fc6b8c102ca55ff38ef70c2f073c5993 9465
        .frontend/stage/StageCommandForm.tsx 2dea1ddde16ec3a946a52bb4a7044928bb25de32b7702089634e48f1157fed5d 22440
        .frontend/stage/StageGuardedAction.tsx 8c950711a7cd89ddd11191f846bf5230da93cf2a31a450976ff22d26e6210c5e 6921
        .frontend/stage/app.css c92f082cdc927eb690b3a3c9e339a89e86f3f0f64c93a51fae96ea51c669ebf8 4463
        .frontend/stage/blueprint.ts 4a671d5d2f8be5f3eb80282a44214945f22075b41f0300196530a77a87ccd055 8846
        .frontend/stage/navigationTree.ts b07b69a65ad734b534cc865355e210c938aa0832057483e4ccdfbd04545fdfc3 7290
        .frontend/stage/stageCommands.ts 3b40fc0d0f4987c4ac3d253acf1b0c33104c0eb43ad9bfa504088d51b0d6d179 1944
        .frontend/stage/stageComponents.tsx 7f9ec92b0f9e25c49d3a37911413c9f4f0f61156fca3d061931e125e728f5145 13558
        .frontend/stage/stageData.tsx d47040fee417d01d666e0bafb10a52e8c3bdc44ea06b05af2a7e8dfd9f06578d 18535
        .frontend/stage/stageDataSources.ts ade1a9a757ec28de294254bcb59ee95df1e03d047036e9281915665c0d19d888 4645
        .frontend/stage/stageGuards.ts d3caad28af719787f1007a8824898c86e6a4ac2eba863c36c4afaaae45b4cdf1 9330
        .frontend/stage/stageNavigation.ts 46fc13d64e68d906f47a2c4fb92ade4ed61535b3ec63ca40624d4fe2c9437633 2529
        .frontend/stage/stageRoutes.ts 7d85720c1d04fbf81da0bb6a287b67a58fa5ec6e275f04595250c16cc4b9a348 1792
        .frontend/stage/stageRowInteractions.ts 11d46c9b7e3dc182feb9629eb21d3d5b181b4119d3a03c4e565b814bfc2caf6f 3600
        .frontend/stage/stageSource.ts 0d8df29508315a5d17abe3bcf928bef4a1b05f1115395bfbe3356c3df7569817 4127
        .frontend/stage/stageTheme.ts 41c903960761179212feba27f3e3352666c54a6364b3b0a2223589e028df4398 1097
        .frontend/stage/useSizeClass.ts e7692466d18b3330ef13abe04ded391bdcaf3ab0781fa93290fc42ba5f3ea068 1095
        .frontend/stage/useStrings.ts 7e9b26328e986b05f504ed40ae0656236d023b01ea4a1bf790220dd2f2309e17 2449
        .frontend/stage/vite-env.d.ts e24655f2bbc165e2dcbd988edb4a9a28f50fb3d2d9d78c775c7d0ee144bcce10 498
        .frontend/styledPrimeReact.ts 411262e641b07da4957bb52bf84deecdeef6319dbe64a1f1cba828065b402feb 3204
        .frontend/tsconfig.json efa4d0a8fdc813c520c240d2df0cd9211132fcde952f3d21f244c1a74ab124bf 1013
        .frontend/tsconfig.node.json 8d6d5e5ed11a91fdfa530d705f55c9676f62f12e72aa21b45502ecbadc27607b 313
        .frontend/vite.config.ts f4c1f53dda10136c81243564eee59796c43cbf8485479de9d24fdcb8431b79bc 1354
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        Common/CommentId.cs 93d1d308af3ac96f3e273feb88a520dba769d3b34c659ea6c610b85029f48911 662
        Common/WorkItemId.cs 55bd6006380b8a2e1984ac65475350657ffbd01354dc14ae074895d64bfe496a 668
        Common/WorkStatus.cs 5bee5282aed332a1c26d9b125b73756d5a5e558d3ff3ced2a4ef0d01cac63978 307
        Directory.Build.props 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props af799a176bf7be0de5e660924f4bde3444170788e9633399a1f65f0b72c9602f 133
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs e5010616b42fcfa3ebea105bd4f76a12423291f8b29f6ebb0035963b59b160f7 996
        Workspaces.csproj eb5183b938695bdf07a42a4c58e83b6855405d6b6f916aea6b24d32ccbae7011 2379
        Workspaces.slnx f74d3ddcde0097a4bdb2ce376128f76d0ed516c309d01f273f9b92c394ba2166 62
        Workspaces/Tracking/AddComment/AddComment.cs e6b1cd4db8918ab4eb9421f51e0c1224213252929dbfe3460a917dea88431c59 1115
        Workspaces/Tracking/AddComment/when_adding_acomment.cs 317e90a82f631bd47dd13a15aaba8c2dae99c23e0459631c7274be2101570c95 1884
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 336c9b53311c6bddde7371b4e2e3018e70aa2b60acf87e614aa9867362d4afd8 1082
        Workspaces/Tracking/CloseWorkItem/when_closing_awork_item.cs ec6f758f35f6e44f595b20f44aed5d1a9d39866acd56c272f1e8cfb1db1aa416 2064
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 17c6b0173d5bb17eb3c5c084ec802f8df5206def555e1e584f629f8b473a58e1 1121
        Workspaces/Tracking/CreateWorkItem/when_creating_awork_item.cs d2bcb0b9a25bd35a5f1c24ba3a9c991301c02d8f7de467cef0ec32ca6d5223f9 1755
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 2bbf45806cd32a070025b607f825f01fa2c60d3ffb70c66baf3313e5602f100b 1033
        Workspaces/Tracking/RenameWorkItem/when_renaming_awork_item.cs 4173f200623020b72a3025e1b96f48e482e5087a7845fd659a73c20a4f70bceb 2084
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 7d9dbf0b84c521e7707b0c3ef4a948d32e75c0f48bbfb058100a3b566cd63905 2230
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_awork_item_is_queried.cs 0cfd7ff1f7ca16fd1786ca639d19525689d1a351e9e4bcb101364c93c64eb445 2543
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs b20d80c6e2c6ecd7f1ca5fe79745386c2608bddb1acb47ea5c7399bb4905268d 2920
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 7348d49d01490ecb86f5b8489b95721c792d565c24b46b3304b851c1d0e91107 2833
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 00f31c1ade25d846d6646b4e0e99a3f9cbdb26a023838fc9bac261ae73275c9b 3220
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs f646a742678761c49e10be6d010dfe8f52bb2d0f615348dd724e202e122ed70d 2502
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml 87b60b599fe2b79b72212d6b1547331e502d84c9a57004ae920ae3c71c0571e6 259
        package.json a8593c8f788e285c0e9d4e563640141e11d7759f8cdd545008e9719d9b871f9e 1431
        scene.json 61125b4580a21576f69fae0dc03e94ef9abea2302101e8fab9b937c7827ca57c 3918
        src/bindings.ts acd1c3406a5f7a8cdd640e3682001634290a1e3359c63dfe86494badec891833 1467
        src/stage-scene.json 61125b4580a21576f69fae0dc03e94ef9abea2302101e8fab9b937c7827ca57c 3918
        src/stage.ts 2e7593c3453949763a6aff1e189c57a4c1b90ec049bfabb1090674e89ba7a8b8 1745
        tsconfig.json d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb 47
        """;

    public static TheoryData<string> Forms => [.. ScreenCompositionCorpus.V1.SourceForms.Select(form => form.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_without_diagnostics(string form) => PlanOf(form).Diagnostics.ShouldBeEmpty();

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_exactly_the_pinned_ordered_artifacts(string form) =>
        PlanOf(form).Artifacts.Select(artifact => $"{artifact.RelativePath} {artifact.Sha256} {artifact.Bytes.Length}").ShouldEqual(Pinned());

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_pinned_digest(string form) => PlanOf(form).Digest.ShouldEqual(Digest);

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_plan_the_same_bytes_as_the_folder_form(string form) =>
        PlanOf(form).Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())).ShouldEqual(PlanOf("folder").Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())));

    [Fact] void should_cover_every_source_form() => Forms.Count.ShouldEqual(4);

    static string[] Pinned() => [.. Manifest.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())];

    static ArtifactRenderPlan PlanOf(string name)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == name);
        return Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: false).Plan;
    }
}
