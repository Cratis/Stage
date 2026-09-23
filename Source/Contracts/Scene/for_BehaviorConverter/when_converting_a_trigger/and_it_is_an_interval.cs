// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter.when_converting_a_trigger;

/// <summary>
/// Screenplay lets an interval be authored in whichever unit reads best. The model carries seconds, so a
/// renderer scheduling a timer never has to know units existed.
/// </summary>
public class and_it_is_an_interval : Specification
{
    SceneModel.IntervalInteractionTrigger _seconds = null!;
    SceneModel.IntervalInteractionTrigger _minutes = null!;
    SceneModel.IntervalInteractionTrigger _hours = null!;
    SceneModel.IntervalInteractionTrigger _days = null!;

    static SceneModel.IntervalInteractionTrigger Convert(int amount, IntervalUnit unit) =>
        (SceneModel.IntervalInteractionTrigger)BehaviorConverter.ConvertTrigger(
            new IntervalInteractionTriggerSyntax(amount, unit, SourceLocation.Start));

    void Because()
    {
        _seconds = Convert(30, IntervalUnit.Seconds);
        _minutes = Convert(5, IntervalUnit.Minutes);
        _hours = Convert(2, IntervalUnit.Hours);
        _days = Convert(1, IntervalUnit.Days);
    }

    [Fact] void should_keep_seconds_as_they_are() => _seconds.Seconds.ShouldEqual(30);
    [Fact] void should_express_minutes_in_seconds() => _minutes.Seconds.ShouldEqual(300);
    [Fact] void should_express_hours_in_seconds() => _hours.Seconds.ShouldEqual(7200);
    [Fact] void should_express_days_in_seconds() => _days.Seconds.ShouldEqual(86400);
}
