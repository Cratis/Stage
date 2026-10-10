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
    const string Digest = "f48a9ac3cce13943ebbb9eff2cf317ac074e065ebca2c2b346d72d5744f67f04";

    const string Manifest = """
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx 61532310e2937a19d6b76d50faa5623e6af3942e35566111a7a89051820a4287 2055
        .frontend/stage/App.tsx 8b04e690b307eb808f6f4231468691d050b03d60d1e44ba9fa37b90ad836284b 15525
        .frontend/stage/FlowArrangementView.tsx 828b0518dd9d98a23605452355cb9a8a4e070bf98f61b6c014b0f705585cec93 2792
        .frontend/stage/StageChrome.tsx a23311d5824fc19e1dd8015f387fdc35e8a0606a23927451dfafd3ae268516af 4933
        .frontend/stage/StageCommandForm.tsx 0733683713edacb617fbf4e89ddcca9d47de7c2f955cfaa336eb1d20884eb97a 20149
        .frontend/stage/StageGuardedAction.tsx d3895a09668a3226a99ffaf19431fe591356456a622727c896ce11ef956b3189 6879
        .frontend/stage/app.css 42eb2fa27e29780a4d0df89cbac8f4204754dc641f5719e0c656e12615a5381b 3410
        .frontend/stage/blueprint.ts 5b63faeb807b97da4f534afc3c050292ca914931955f498ce55a76c600a37885 6594
        .frontend/stage/stageCommands.ts 3b40fc0d0f4987c4ac3d253acf1b0c33104c0eb43ad9bfa504088d51b0d6d179 1944
        .frontend/stage/stageComponents.tsx 095fc186c147e425d80cafcc8fc89393d731899a76156da59f6e2e1c63f46cf9 12557
        .frontend/stage/stageData.tsx 56b88184a1db86fbccd11231c8be8562c3834b6f3406fdd1f0e084e19d38bce4 17805
        .frontend/stage/stageDataSources.ts ade1a9a757ec28de294254bcb59ee95df1e03d047036e9281915665c0d19d888 4645
        .frontend/stage/stageGuards.ts dc90fb82a717a1105eaa6749a6dae72d232c8d64091e0a6e591f8f962ac962bc 7107
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
        Workspaces.csproj 2328c0c2f682dbe16d3b4e40459a7439be7654daed2d43000881c6c12faff81b 2379
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
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_awork_item_is_queried.cs 40a5a1c31b391ea067305bd610ff41f96e23c030ab5566ff2d4929059ff2efe7 2465
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs b20d80c6e2c6ecd7f1ca5fe79745386c2608bddb1acb47ea5c7399bb4905268d 2920
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs 7acd624c3623b2a1a5f9a35ca37842a60bb20ec3f43e8951b1b3e2d1bd5e1bc7 2755
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 00f31c1ade25d846d6646b4e0e99a3f9cbdb26a023838fc9bac261ae73275c9b 3220
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs fdbc398936fe6c3a5fd324deaeda4d06ab7471822bedd879cbd2ed1faa3227b7 2424
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml 859dad3906953dad53c7c4d6a3e51a3ee037cdbe9a1a6bff104b92079787e2c0 259
        package.json 538002df101a3086c889645afac4d7d23e038505031d477e969af861df0c0be5 1431
        scene.json f324f8408a350075c3bcd5543dcdf66c56bd98330cb19ba82902937a150f7800 3908
        src/bindings.ts acd1c3406a5f7a8cdd640e3682001634290a1e3359c63dfe86494badec891833 1467
        src/stage-scene.json f324f8408a350075c3bcd5543dcdf66c56bd98330cb19ba82902937a150f7800 3908
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
