// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Exposure;
using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The corpus's positive typed composition source - exposures, instance values, screen contributions, template
/// content and picker metadata, grid/grow/span arrangements and a navigation outlet - translated to Scene by Stage.
/// </summary>
/// <remarks>
/// Each construct is asserted on its own Scene type, never on an element's open property bag, and the canonical
/// bytes are pinned so a change to any of them is a visible change to this file.
/// </remarks>
public class when_translating_the_typed_screen_composition : given.the_planned_screen_composition_corpus
{
    SceneApplication _scene = null!;
    byte[] _bytes = null!;

    void Because()
    {
        _scene = SceneOf([ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "screen-release-ui-positive").Document]);
        _bytes = Encoding.UTF8.GetBytes(CanonicalSceneJson.Serialize(_scene));
    }

    [Fact] void should_pin_the_canonical_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("3eadfebf65c31808b16b1f802ad230bd48517ff2f7fe4a58f76ce2c110fad717");
    [Fact] void should_pin_the_canonical_scene_length() => _bytes.Length.ShouldEqual(18453);

    [Fact] void should_declare_what_each_template_exposes() => _scene.Exposures.Select(exposure => exposure.Owner).ShouldContainOnly(["WorkspaceFeatureShell", "EditWorkItemDialog"]);
    [Fact] void should_expose_a_collection_with_its_operations() => Exposed("WorkspaceFeatureShell", "actions").Operations!.ShouldContainOnly([CollectionOperation.Add, CollectionOperation.Remove, CollectionOperation.Reorder, CollectionOperation.EditFields]);
    [Fact] void should_restrict_the_editable_fields_of_a_collection() => Exposed("WorkspaceFeatureShell", "actions").EditableFields!.ShouldContainOnly(["label", "icon"]);
    [Fact] void should_carry_the_exposed_label() => Exposed("WorkspaceFeatureShell", "title").Label.ShouldEqual("Header title");
    [Fact] void should_carry_a_re_exposure() => Exposed("EditWorkItemDialog", "title").ReExposes.ShouldEqual("WorkspaceFeatureShell");

    [Fact] void should_store_an_instance_value() => Contribution("title").Value!.Value.GetString().ShouldEqual("Work items");
    [Fact] void should_store_instance_items_in_order() => Contribution("actions").Items.Select(item => item.Id).ShouldEqual(["export:csv", "archive"]);
    [Fact] void should_store_an_item_field_value() => Contribution("actions").Items[0].Values["label"].GetString().ShouldEqual("Export");

    [Fact] void should_carry_the_template_display_name() => Shell.DisplayName.ShouldEqual("Workspace feature shell");
    [Fact] void should_carry_the_template_picker_category() => Shell.Metadata!.Category.ShouldEqual("feature");
    [Fact] void should_carry_the_template_scopes() => Shell.Metadata!.Scopes!.ShouldContainOnly([TemplateScope.Module, TemplateScope.Feature, TemplateScope.Subfeature, TemplateScope.Slice]);
    [Fact] void should_carry_the_template_slot_content() => Shell.Content!["header"].Single().Id.ShouldEqual("shell:header");

    [Fact] void should_arrange_a_grid_with_its_tracks() => Grid.Columns.ShouldEqual(2);
    [Fact] void should_weigh_a_grid_inside_its_parent() => Grid.Grow.ShouldEqual(1d);
    [Fact] void should_weigh_each_grid_slot() => Grid.Children.OfType<FlowSlotLeaf>().Select(slot => $"{slot.SlotName}:{slot.Grow}").ShouldEqual(["list:1", "detail:2"]);
    [Fact] void should_span_each_grid_slot() => Grid.Children.OfType<FlowSlotLeaf>().Select(slot => slot.Span).ShouldEqual([1, 1]);

    [Fact] void should_add_the_screens_own_contributions_after_its_scopes() => _scene.Screens.Single(screen => screen.Name == "WorkItemList").Contributions.Select(contribution => contribution.ContributionPointName).ShouldEqual(["Navigation", "WorkspaceActions", "Navigation"]);
    [Fact] void should_navigate_into_an_outlet() => Destination.Outlet.ShouldEqual("details");
    [Fact] void should_mark_the_destination_as_an_outlet() => Destination.Kind.ShouldEqual(DestinationKind.Outlet);
    [Fact] void should_bind_the_route_parameter() => Destination.RouteParameterBindings!["workItemId"].ComponentPropertyPath.ShouldEqual("selectedItem.workItemId");

    ScreenTemplate Shell => _scene.ScreenTemplates.Single(template => template.Name == "WorkspaceFeatureShell");
    FlowGrid Grid => All(((FlowArrangement)Shell.Arrangement!).Root).OfType<FlowGrid>().Single();
    ExposedProperty Exposed(string owner, string path) => _scene.Exposures.Single(exposure => exposure.Owner == owner).Properties.Single(property => property.Path == path);
    InstanceContribution Contribution(string path) => _scene.InstanceContributions.Single(contribution => contribution.Instance == "WorkItemList" && contribution.Path == path);

    DestinationReference Destination => _scene.Screens
        .SelectMany(screen => screen.SlotContent.Values.SelectMany(elements => elements).Concat(screen.Contributions.Select(contribution => contribution.Content)))
        .SelectMany(Elements)
        .Select(element => element.Properties.GetValueOrDefault(SceneElementProperties.Destination))
        .OfType<DestinationReference>()
        .Single(destination => destination.Outlet is not null);

    static IEnumerable<FlowNode> All(FlowNode node) =>
        node is FlowContainer container ? [node, .. container.Children.SelectMany(All)] : [node];

    static IEnumerable<ExternalComponent> Elements(SceneElement element) =>
        element is ExternalComponent component ? [component, .. component.Slots.Values.SelectMany(slot => slot).SelectMany(Elements)] : [];
}
