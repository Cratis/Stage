// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_an_arrangement_is_unsupported : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        Layouts = [_application.Layouts[0] with { Arrangement = new UnknownArrangementType() }]
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_report_an_unsupported_arrangement_for_each_target() => _result.Targets.All(target => target.Findings.Any(finding => finding.Kind == RenderFindingKind.ArrangementUnsupported)).ShouldBeTrue();
    [Fact] void should_not_misreport_a_missing_variant() => _result.Targets.SelectMany(target => target.Findings).Any(finding => finding.Kind == RenderFindingKind.SizeClassVariantMissing).ShouldBeFalse();
    [Fact] void should_name_the_code_and_arrangement_type() => _result.Targets[0].Findings.Single(finding => finding.Kind == RenderFindingKind.ArrangementUnsupported).Message.ShouldContain("STAGE-SCENE-ARRANGEMENT-003: arrangement UnknownArrangementType");

    sealed record UnknownArrangementType : Arrangement;
}
