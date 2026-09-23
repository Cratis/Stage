// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// Rejects declarations that legacy model-bound attributes would otherwise silently simplify.
/// </summary>
public class when_the_projection_loses_what_no_attribute_carries : Specification
{
    ApplicationSet _applicationSet = null!;
    Exception? _error;

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

    void Because() => _error = Catch.Exception(() => new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp"));

    [Fact] void should_reject_unfaithful_block_settings() => _error.ShouldBeOfExactType<UnsupportedLegacyProjection>();
    [Fact] void should_name_the_blocking_diagnostic() => _error!.Message.ShouldContain(UnsupportedLegacyProjection.DiagnosticCode);

    static EventSyntax Event(string name, string property) =>
        new(
            name,
            [new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);
}
