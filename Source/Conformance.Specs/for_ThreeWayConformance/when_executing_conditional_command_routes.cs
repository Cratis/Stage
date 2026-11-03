// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_conditional_command_routes : a_generated_application
{
    string _warnings;
    GeneratedTestResult[] _results;

    protected override ArtifactRenderPlan CreatePlan() => routed_specifications.Plan(conditional_routed_specifications.Model());

    async Task Because()
    {
        try
        {
            _warnings = BuildWarnings(await Run("conditional-route-build.log", "build", "Routed.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("conditional-route-test.log", "test", "Routed.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=conditional-route.trx", "--results-directory", _evidence.FullName);
            _results = [.. GeneratedTestResults.Read(XDocument.Load(Path.Combine(_evidence.FullName, "conditional-route.trx")))];
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact] void should_compile_the_guarded_property_route_without_warnings() => _warnings.ShouldBeEmpty();
    [Fact] void should_pass_the_generated_specifications() => _results.All(result => result.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_assert_exactly_one_taken_event() => _results.Any(result => result.Matches(methodName: "should_append_exactly_1_events")).ShouldBeTrue();
    [Fact] void should_reject_the_empty_route() => _results.Any(result => result.Matches(className: "when_rejecting_empty_route", methodName: "should_have_validation_errors")).ShouldBeTrue();
}
#endif
