// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_package_dependency_is_missing : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _catalog = [.. _catalog.Where(package => package.Name != "Tailwind")];

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_not_consider_the_application_renderable() => _result.IsComplete.ShouldBeFalse();
    [Fact] void should_report_the_dependency_nothing_satisfies() => Finding()!.Kind.ShouldEqual(RenderFindingKind.PackageDependencyMissing);
    [Fact] void should_name_the_package_that_is_missing() => Finding()!.Subject.ShouldEqual("Tailwind");
    [Fact] void should_leave_it_out_of_the_resolved_package_order() => _result.Targets[0].Profile.Packages.ShouldContainOnly("PrimeReact");

    RenderFinding? Finding() =>
        _result.Targets[0].Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.PackageDependencyMissing);
}
