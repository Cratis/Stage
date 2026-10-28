// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_a_later_file_fails_to_render : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new();
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _language.RefusedPaths["Commands/Echo.test"] = given.a_test_language.AmbiguousName;
        _plan = PlanWithLanguage(new() { ["templates/reaction.sbn"] = "{{ refuse 'trust' 'unsafe' }}" }, _language);
    }

    [Fact] void should_report_the_template_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.Refuse]);
    [Fact] void should_not_call_the_language() => _language.Calls.ShouldBeEmpty();
}
