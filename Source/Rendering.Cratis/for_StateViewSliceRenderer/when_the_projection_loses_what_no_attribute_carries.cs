// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// Three losses that no model-bound attribute can carry. Each renders something that still compiles, so none of
/// them can be left to the compiler to catch — the renderer's contract is that what it cannot express is reported
/// rather than silently dropped, and these are the cases where a silent drop would invert or hollow out what the
/// author wrote.
/// </summary>
public class when_the_projection_loses_what_no_attribute_carries : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;

    void Establish()
    {
        var opened = Event("AccountOpened", "reference");
        var reopened = Event("AccountReopened", "reference");
        var renamed = Event("HolderRenamed", "name");

        // Two events, one mapping pass: only the first contributes a mapped property.
        var from = new FromSyntax(
            [
                new EventSpecSyntax("AccountOpened", null, SourceLocation.Start),
                new EventSpecSyntax("AccountReopened", null, SourceLocation.Start),
            ],
            null,
            null,
            [new SetMappingSyntax("reference", new PathExpressionSyntax("reference", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        // Matches on a property nothing maps onto the read model.
        var join = new JoinSyntax(
            "holder",
            "holderId",
            [
                new JoinEventSyntax(
                    "HolderRenamed",
                    AutoMapMode.Disabled,
                    [new SetMappingSyntax("holderName", new PathExpressionSyntax("name", SourceLocation.Start), SourceLocation.Start)],
                    SourceLocation.Start)
            ],
            SourceLocation.Start);

        var every = new EverySyntax(
            [new SetMappingSyntax("lastSeenAt", new EventContextExpressionSyntax("occurred", SourceLocation.Start), SourceLocation.Start)],
            true,
            AutoMapMode.Disabled,
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "AccountSummary",
            "AccountSummary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("reference", SourceLocation.Start), SourceLocation.Start),
            [from, join, every],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "AccountSummary", [opened, reopened, renamed], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Accounts", [], [slice], SourceLocation.Start);
        _applicationSet = new ApplicationSet(
            [new ApplicationSyntax([], [], [], [new ModuleSyntax("Banking", [], [feature], SourceLocation.Start)], SourceLocation.Start)]);
    }

    void Because() => _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");

    [Fact] void should_report_that_only_the_first_event_of_a_multi_event_from_is_mapped() =>
        _file.Diagnostics.ShouldContain(
            "The 'from' block naming 'AccountOpened', 'AccountReopened' maps only from 'AccountOpened' — " +
            "the other events are subscribed but contribute no mapped property.");

    [Fact] void should_still_subscribe_to_the_unmapped_event() => _file.Content.ShouldContain("[FromEvent<AccountReopened>]");

    [Fact] void should_report_a_join_matching_a_property_nothing_maps() =>
        _file.Diagnostics.ShouldContain(
            "The join on 'holderId' matches 'HolderId', which no block maps onto the read model — " +
            "it is referenced by name and only checked when the projection is built.");

    [Fact] void should_report_that_block_level_no_automap_is_not_carried() =>
        _file.Diagnostics.ShouldContain(
            "'no automap' on the every, join block(s) is not rendered — a model-bound [NoAutoMap] is scoped to the " +
            "read model or a single property, never to one block, so those blocks map under the projection's own setting.");

    static EventSyntax Event(string name, string property) =>
        new(
            name,
            [new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);
}
