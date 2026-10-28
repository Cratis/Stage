// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_the_language_sees_every_file : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new();
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _plan = PlanWithLanguage([], _language);
    }

    [Fact] void should_call_the_language_once() => _language.Calls.Count.ShouldEqual(1);
    [Fact] void should_pass_every_path_in_output_order() => string.Join("|", _language.Calls.SelectMany(_ => _)).ShouldEqual("Commands/Shout.test|Commands/Echo.test|Reactions/EchoReaction.test|Index.test");
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
}
