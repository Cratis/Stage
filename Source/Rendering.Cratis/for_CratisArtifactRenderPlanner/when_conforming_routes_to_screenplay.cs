// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc;
using Cratis.Screenplay.Api;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Sdk;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_conforming_routes_to_screenplay : a_rendered_application_with_api_routes
{
    IReadOnlyList<(string Method, string Route)> _routes = [];

    void Because() => _routes = Routes(ScaffoldOptions());

    [Fact] void should_match_every_generated_operation_and_validation_route() => AssertConformance(_routes);

    [Fact]
    void should_cover_single_and_multi_command_slices() => _description.Operations
        .Where(operation => operation.Kind == ApiOperationKind.Command).Select(operation => operation.Name)
        .ShouldContainOnly("RecordPayment", "CorrectPayment", "CancelPayment");

    [Fact]
    void should_cover_queries_including_an_escaped_method()
    {
        _description.Operations.Where(operation => operation.Kind == ApiOperationKind.Query).Select(operation => operation.Name)
            .ShouldContainOnly("PaymentById", "Total");
        _assembly.GetTypes().Single(type => type.Name == "PaymentTotals").GetMethod("Total_").ShouldNotBeNull();
    }

    [Fact] void should_report_a_changed_route_prefix() => AssertDrift(options => options.RoutePrefix = "changed");
    [Fact] void should_report_a_changed_namespace_skip() => AssertDrift(options => options.SegmentsToSkipForRoute = 0);
    [Fact] void should_report_a_changed_command_name_setting() => AssertDrift(options => options.IncludeCommandNameInRoute = true);
    [Fact] void should_report_a_changed_query_name_setting() => AssertDrift(options => options.IncludeQueryNameInRoute = false);

    void AssertConformance(IEnumerable<(string Method, string Route)> routes)
    {
        var expected = _description.Operations.Select(operation => (operation.Method, operation.Route))
            .Concat(_description.Operations.Where(operation => operation.ValidationRoute is not null)
                .Select(operation => ("POST", operation.ValidationRoute!)));
        Assert.Equal(expected.Order().ToArray(), routes.Order().ToArray());
    }

    void AssertDrift(Action<ApiEndpointOptions> change)
    {
        var options = ScaffoldOptions();
        change(options.GeneratedApis);
        Assert.Throws<EqualException>(() => AssertConformance(Routes(options)));
    }
}
#endif
