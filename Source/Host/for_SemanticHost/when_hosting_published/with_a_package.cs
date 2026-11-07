// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Cratis.Stage.Publishing;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_published;

public class with_a_package : given.a_semantic_host
{
    void Establish() => _arguments = ["--published", "--Stage:Sandbox:ForwardedIdentity=true"];

    async Task Because()
    {
        var manifest = new PackageManifest(1, "App", "app", "1.0.0", "1.0.0", "pub-1", "sha256:0", false, new AuthenticationRequirements(true, [], [], [], false), []);
        var options = new PublishedOptions("/package", "mongodb://localhost", "TrustedProxy", ["127.0.0.1"], "app.example", false, null, null);
        await BuildHost(new PublishedPackage("/package", manifest), options);
    }

    [Fact] void should_admit_the_model() => _refused.ShouldBeFalse();
    [Fact] void should_map_model_references_as_runtime() => _patterns.ShouldContain("/stage/model-references");
    [Fact] void should_keep_approved_runtime_packages() => _patterns.ShouldContain("/stage/packages");
    [Fact] void should_only_map_runtime_stage_routes() => _patterns.Where(pattern => pattern.StartsWith("/stage/", StringComparison.Ordinal)).ShouldContainOnly(["/stage/status", "/stage/packages", "/stage/scene", "/stage/routes", "/stage/locales", "/stage/strings/{locale}", "/stage/model-references"]);
    [Fact] void should_not_map_any_tooling() => PublishedSurface.ToolingRoutes(((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints)).ShouldBeEmpty();
    [Fact] void should_disable_personas() => _app.Services.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldBeFalse();
    [Fact] void should_not_redirect_to_the_read_model_page() => _readModelPage.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
}
