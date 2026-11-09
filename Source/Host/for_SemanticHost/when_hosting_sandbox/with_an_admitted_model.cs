// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class with_an_admitted_model : when_hosting_published.given.a_semantic_host
{
    async Task Because() => await BuildHost();

    [Fact] void should_admit_the_model() => _refused.ShouldBeFalse();
    [Fact]
    void should_keep_the_complete_endpoint_set() => _patterns.Distinct().ShouldContainOnly(
    [
        "/stage/packages", "/stage/load", "/stage/semantic/admission", "/stage/scene", "/stage/routes",
        "/stage/locales", "/stage/strings/{locale}", "/stage/status", "/stage/semantic/specifications/run", "/stage/semantic/journeys/replay",
        "/stage/lifecycles", "/stage/lifecycles/workflows/{workflow}/subjects/{subject}/approvals/{node}",
        "/stage/read-models", "/stage/read-models/{id}", "/openapi/{documentName}.json",
        "/workbench/{**path}", "/workbench", "{*path:nonfile}",
        "/api/projects/portfolio/registration/register-project/register-project",
        "/api/projects/portfolio/registration", "/api/projects/portfolio/registration/register-project/register-project/validate",
        "/api/projects/portfolio/registration/validate",
        "/api/projects/portfolio/registration/project-lookup/get-project-summary-by-id",
        "/api/projects/portfolio/registration/get-project-summary-by-id",
        "/api/projects/portfolio/registration/project-lookup/all-project-summaries",
        "/api/projects/portfolio/registration/all-project-summaries",
        "/api/projects/portfolio/registration/project-lookup/project-by-id", "/api/projects/portfolio/registration/project-by-id",
        "/.cratis/queries/health", "/.cratis/identity-details/schema", "/.cratis/users", "/.cratis/tenants", "/.cratis/me",
        "/.cratis/commands", "/.cratis/queries", "/.cratis/queries/ws", "/.cratis/queries/sse",
        "/.cratis/queries/sse/subscribe", "/.cratis/queries/sse/unsubscribe",
        "/scalar/scalar.js", "/scalar/scalar.aspnetcore.js", "/scalar/favicon.svg", "/scalar/{documentName?}"
    ]);
    [Fact] void should_keep_all_sandbox_stage_routes() => _patterns.Where(pattern => pattern.StartsWith("/stage/", StringComparison.Ordinal)).ShouldContainOnly(["/stage/status", "/stage/packages", "/stage/scene", "/stage/routes", "/stage/locales", "/stage/strings/{locale}", "/stage/load", "/stage/semantic/admission", "/stage/semantic/specifications/run", "/stage/semantic/journeys/replay", "/stage/read-models", "/stage/read-models/{id}", "/stage/lifecycles", "/stage/lifecycles/workflows/{workflow}/subjects/{subject}/approvals/{node}"]);
    [Fact] void should_keep_the_read_model_redirect() => _readModelPage.Response.StatusCode.ShouldEqual(StatusCodes.Status302Found);
    [Fact] void should_redirect_to_the_read_model_page() => _readModelPage.Response.Headers.Location.ToString().ShouldEqual("../read-models");
    [Fact] void should_keep_openapi() => _patterns.Any(pattern => pattern.Contains("openapi", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_keep_scalar() => _patterns.Any(pattern => pattern.Contains("scalar", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_keep_both_workbench_routes() => _patterns.Where(pattern => pattern.StartsWith("/workbench", StringComparison.Ordinal)).ShouldContainOnly(["/workbench", "/workbench/{**path}"]);
    [Fact] void should_keep_application_apis() => _patterns.Any(pattern => pattern.StartsWith("/api/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_arc_discovery() => _patterns.Any(pattern => pattern.StartsWith("/.cratis/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_map_package_model_references() => _patterns.ShouldNotContain("/stage/model-references");
    [Fact] void should_keep_the_spa_fallback() => _patterns.ShouldContain("{*path:nonfile}");
}
