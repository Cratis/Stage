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
    const string Digest = "2ee13d9738f309b25e7dab287260ad2e692ba97f63b44511f2c14ae311c16cc9";

    const string Manifest = """
        .frontend/index.css c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2 436
        .frontend/index.html 4b88419fe62ae4b9ef8582d1fc203dd9084959d875964031018175ed24cc856a 329
        .frontend/main.tsx e22844dd5715b58b06cb63cfd4a3fd6a610aebeaf623f0e60e4c0e7f02943ef6 3308
        .frontend/tsconfig.json 8188daef31c13c81639fd8e50c46f580322575bb5f34d033b486df2a14366e21 939
        .frontend/tsconfig.node.json 3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342 290
        .frontend/vite.config.ts 54adb7473f5e230c9ded78b7549357a6e7abb4a547cf11d6094d2742df4002e5 1088
        .gitignore 74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9 253
        Common/CommentId.cs 93d1d308af3ac96f3e273feb88a520dba769d3b34c659ea6c610b85029f48911 662
        Common/WorkItemId.cs 55bd6006380b8a2e1984ac65475350657ffbd01354dc14ae074895d64bfe496a 668
        Common/WorkStatus.cs 5bee5282aed332a1c26d9b125b73756d5a5e558d3ff3ced2a4ef0d01cac63978 307
        Directory.Build.props 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Build.targets 17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea 12
        Directory.Packages.props af799a176bf7be0de5e660924f4bde3444170788e9633399a1f65f0b72c9602f 133
        GeneratedPolicyRegistration.cs 05e8d5a16d52bc9e50828e14aab5d929e5778f7f855b3f8e8ec4c4e7d122ddbf 742
        Program.cs e5010616b42fcfa3ebea105bd4f76a12423291f8b29f6ebb0035963b59b160f7 996
        Workspaces.csproj 556324aad5c61f83eda63a97701cd8f1e2d57e4d2743cb51f4d0e9eefec074f1 2379
        Workspaces.slnx f74d3ddcde0097a4bdb2ce376128f76d0ed516c309d01f273f9b92c394ba2166 62
        Workspaces/Tracking/AddComment/AddComment.cs e6b1cd4db8918ab4eb9421f51e0c1224213252929dbfe3460a917dea88431c59 1115
        Workspaces/Tracking/AddComment/when_adding_acomment.cs 317e90a82f631bd47dd13a15aaba8c2dae99c23e0459631c7274be2101570c95 1884
        Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs 2c07081000d71273ec2f2d2a8d7ff8ec110856615c9488e68a54757dce094ec1 1089
        Workspaces/Tracking/CloseWorkItem/when_closing_awork_item.cs fb3b6728b350355dfdd886378266f9180adedf415dd69cb0177ef8d8c0b3482e 2078
        Workspaces/Tracking/CreateWorkItem/CreateWorkItem.cs 2cecfc09b2723365e70269daba99b7197237a0a3bfa3dc719e9f7866ecd57247 1128
        Workspaces/Tracking/CreateWorkItem/when_creating_awork_item.cs f1b2e036486d9619d76d532cb6fff695fc4b23c83a6e23105f88c4116481d354 1762
        Workspaces/Tracking/RenameWorkItem/RenameWorkItem.cs 2bbf45806cd32a070025b607f825f01fa2c60d3ffb70c66baf3313e5602f100b 1033
        Workspaces/Tracking/RenameWorkItem/when_renaming_awork_item.cs 5e291f4118c6e24424e0e80de457cda915f33836eefab5933f3657dc43a7c069 2091
        Workspaces/Tracking/WorkItemComments/WorkItemComments.cs 7d9dbf0b84c521e7707b0c3ef4a948d32e75c0f48bbfb058100a3b566cd63905 2230
        Workspaces/Tracking/WorkItemComments/when_showing_comments_for_awork_item_is_queried.cs fa37d0283db5ae00df920f249566b5a982f1b5f1bbd6dd6a250c8ff426281520 2547
        Workspaces/Tracking/WorkItemDetails/WorkItemDetails.cs b20d80c6e2c6ecd7f1ca5fe79745386c2608bddb1acb47ea5c7399bb4905268d 2920
        Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs dc9e8bd1eb09f975d21909f6cb5e914afaf2f0aa36a70676070054cb70390efa 2907
        Workspaces/Tracking/WorkItemList/WorkItemList.cs 00f31c1ade25d846d6646b4e0e99a3f9cbdb26a023838fc9bac261ae73275c9b 3220
        Workspaces/Tracking/WorkItemList/when_listing_created_work_items_is_queried.cs 0c07a5229c200dfabbac816bbc2253fb7177443bdbdd33370a13f54c6d30b6e0 2573
        appsettings.json 43edcdf830ab948374c5c6ca2a700b2eed65134267d1830119cb803620cfd9cf 442
        docker-compose.yml d780b4894c6936918f81d04adeef5444c3a7b589883a5fefb06762a6308f2222 259
        package.json cfcae45a47787c90a70f7e6dbd6d47b23c869915d670bd8826c355e3abc2130f 1431
        scene.json f324f8408a350075c3bcd5543dcdf66c56bd98330cb19ba82902937a150f7800 3908
        src/bindings.ts acd1c3406a5f7a8cdd640e3682001634290a1e3359c63dfe86494badec891833 1467
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
