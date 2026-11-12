// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class with_a_package_the_deployment_does_not_allow : for_SemanticHost.given.a_model_path
{
    WebApplication _app = null!;
    HttpClient _client = null!;
    bool _refused;
    string _admission = null!;
    string _listing = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source);
        var digest = "sha256:" + new string('a', 64);
        await File.WriteAllTextAsync(Path.Combine(_directory, "screenplay.lock"), $"""
            lock 1

            extension "acme/mail"
              range "latest"
              resolved "ghcr.io/acme/extension-mail"
              version 1.2.0
              digest {digest}
              contract {digest}
              publisher "https://github.com/acme"
            """);
    }

    async Task Because()
    {
        string[] arguments = ["--urls=http://127.0.0.1:0", $"--Stage:Extensions:Packages:CacheDirectory={Path.Combine(_directory, "cache")}", "--AllowedHosts=*"];
        (_app, var initialize) = await SemanticHost.Build(arguments, _path, hostMode: StageHostMode.Sandbox);
        _refused = initialize is null;
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
        _admission = await _client.GetStringAsync("/stage/semantic/admission");
        _listing = await _client.GetStringAsync("/stage/extensions");
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact] void should_refuse_the_model_before_anything_runs() => _refused.ShouldBeTrue();
    [Fact] void should_report_the_refusal_through_the_admission() => _admission.ShouldContain("acme/mail: NotAllowed");
    [Fact] void should_say_where_to_allow_it() => _admission.ShouldContain(".cratis/extensions.json");
    [Fact] void should_list_the_refusal() => _listing.ShouldContain("NotAllowed");
    [Fact] void should_not_have_downloaded_anything() => Directory.Exists(Path.Combine(_directory, "cache")).ShouldBeFalse();
}
