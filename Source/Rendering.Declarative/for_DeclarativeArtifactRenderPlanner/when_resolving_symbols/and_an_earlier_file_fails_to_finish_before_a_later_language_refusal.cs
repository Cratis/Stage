// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_an_earlier_file_fails_to_finish_before_a_later_language_refusal : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new();
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _language.RefusedPaths["Index.test"] = given.a_test_language.AmbiguousName;
        _plan = PlanWithLanguage(new() { ["templates/command.sbn"] = "before /*__stage_inline:body*/ after" }, _language);
    }

    [Fact] void should_report_the_residue_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([DeclarativeCodes.SentinelResidue]);
}
