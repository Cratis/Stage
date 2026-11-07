// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Cratis.Stage.Publishing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_published;

public class with_conflicting_mode_providers : given.a_semantic_host
{
    DefaultHttpContext _request = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "cratis-stage.json"), """{"Stage":{"Mode":"Sandbox"}}""");
        _arguments = ["--contentRoot", _directory, "--Stage:Mode=Published", "--Stage:Published:Storage:ConnectionString=mongodb://storage:27017", "--Stage:Published:Authentication:Mode=TrustedProxy", "--Stage:Published:TrustedProxies:0=127.0.0.1", "--AllowedHosts=app.example.com"];
    }

    async Task Because()
    {
        var configuration = StageHostModeSelection.EarlyConfiguration(_arguments);
        var mode = StageHostModeSelection.Read(configuration, _arguments, StageRuntimeEngine.Semantic);
        var manifest = new PackageManifest(1, "App", "app", "1.0.0", "1.0.0", "pub-1", "sha256:0", false, new AuthenticationRequirements(true, [], [], [], false), []);
        var options = new PublishedOptions("/package", "mongodb://localhost", "TrustedProxy", ["127.0.0.1"], "app.example", false, null, null);
        await BuildHost(new PublishedPackage("/package", manifest), options, mode);
        _request = new DefaultHttpContext { RequestServices = _app.Services };
        _request.Request.Path = "/unmapped";
        _request.Request.Headers["x-ms-client-principal"] = "untrusted";
        await ((IApplicationBuilder)_app).Build()(_request);
    }

    [Fact] void should_have_a_conflicting_builder_configuration() => _app.Configuration["Stage:Mode"].ShouldEqual("Sandbox");
    [Fact] void should_not_map_tooling() => _patterns.ShouldNotContain("/stage/load");
    [Fact] void should_refuse_the_read_model_page() => _readModelPage.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    [Fact] void should_strip_an_untrusted_principal() => _request.Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeFalse();
}
