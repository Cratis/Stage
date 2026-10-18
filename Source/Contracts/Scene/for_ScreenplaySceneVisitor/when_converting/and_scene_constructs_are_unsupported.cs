// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_converting;

public class and_scene_constructs_are_unsupported : Specification
{
    [Theory]
    [InlineData("directive", "STAGE-SCENE-DIRECTIVE-001")]
    [InlineData("trigger", "STAGE-SCENE-BEHAVIOR-001")]
    [InlineData("trigger-kind", "STAGE-SCENE-BEHAVIOR-001")]
    [InlineData("interval", "STAGE-SCENE-BEHAVIOR-002")]
    [InlineData("action", "STAGE-SCENE-ACTION-001")]
    [InlineData("notification", "STAGE-SCENE-ACTION-002")]
    [InlineData("destination", "STAGE-SCENE-CONTRIBUTION-001")]
    [InlineData("profile-size", "STAGE-SCENE-PROFILE-001")]
    [InlineData("profile-color", "STAGE-SCENE-PROFILE-002")]
    public void should_report_the_omission_and_keep_the_remaining_scene(string construct, string code)
    {
        var location = new SourceLocation(9, 5, "unsupported.play");
        InteractionTriggerSyntax trigger = construct switch
        {
            "trigger" => new UnknownTrigger(location),
            "trigger-kind" => new BuiltInInteractionTriggerSyntax((InteractionTriggerKind)99, location),
            "interval" => new IntervalInteractionTriggerSyntax(1, (IntervalUnit)99, location),
            _ => new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, location)
        };
        InteractionActionSyntax action = construct switch
        {
            "action" => new UnknownAction(location),
            "notification" => new NotifyActionSyntax((NotificationLevel)99, "Message", location),
            _ => new NavigateBackActionSyntax(location)
        };
        var behavior = new BehaviorSyntax(null, [], [new InteractionBindingSyntax(trigger, null, [action], location)], location);
        var bad = new ScreenSyntax("Partial", null, [new ScreenTitleSyntax("Retained", location), construct == "directive" ? new UnknownDirective(location) : new ScreenBehaviorSyntax(behavior, location)], location);
        var good = new ScreenSyntax("Supported", null, [new ScreenTitleSyntax("Supported", location)], location);
        var slice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [], [], [], [bad, good], [], [], location);
        var feature = new FeatureSyntax("Browsing", [], [slice], location);
        var module = new ModuleSyntax("Invoicing", [], [feature], location);
        if (construct == "destination")
        {
            module = module with { Contributions = [new ContributionSyntax("Navigation", null, "Entry", null, location) { Destination = new((ContributionDestinationKind)99, "unknown", location) }] };
        }
        var application = new ApplicationSyntax([], [], [], [module], location);
        if (construct.StartsWith("profile-", StringComparison.Ordinal))
        {
            application = application with { UiProfiles = [new UiProfileSyntax("Bad", ["web"], construct == "profile-size" ? "unknown" : null, [], location) { Appearance = construct == "profile-color" ? new ProfileAppearanceSyntax("unknown", null, location) : null }, new UiProfileSyntax("Good", ["web"], null, [], location)] };
        }

        var result = new ScreenplaySceneVisitor().Convert(application);

        result.Screens.Count.ShouldEqual(2);
        result.Screens[0].SlotContent[DefaultLayout.ContentSlotName].Count.ShouldEqual(1);
        if (construct.StartsWith("profile-", StringComparison.Ordinal)) result.UiProfiles.Single().Name.ShouldEqual("Good");
        result.ConversionDiagnostics.Count.ShouldEqual(1);
        result.ConversionDiagnostics[0].Code.ShouldEqual(code);
        result.ConversionDiagnostics[0].Severity.ShouldEqual(DiagnosticSeverity.Error);
        result.ConversionDiagnostics[0].Location.ShouldEqual(location);
        result.ConversionDiagnostics[0].Path.ShouldNotBeEmpty();
        result.ConversionDiagnostics[0].Construct.ShouldNotBeEmpty();
        Catch.Exception(() => new ScreenplaySceneVisitor().Visit(application)).ShouldNotBeNull();
    }

    sealed record UnknownDirective(SourceLocation Location) : ScreenDirectiveSyntax(Location);
    sealed record UnknownTrigger(SourceLocation Location) : InteractionTriggerSyntax(Location);
    sealed record UnknownAction(SourceLocation Location) : InteractionActionSyntax(Location);
}
