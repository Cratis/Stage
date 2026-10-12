// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_serving_the_packages : given.a_packages_folder
{
    WebApplication _app = null!;
    HttpClient _client = null!;
    string _document = null!;
    HttpResponseMessage _module = null!;
    HttpResponseMessage _style = null!;
    HttpResponseMessage _unapproved = null!;
    HttpResponseMessage _unknownType = null!;
    HttpResponseMessage _missing = null!;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration[StagePackageEndpoints.PathKey] = Root;
        _app = builder.Build();
        StagePackageEndpoints.Map(_app);
        await _app.StartAsync();

        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
        _document = await _client.GetStringAsync("/stage/packages");
        _module = await _client.GetAsync("/stage/packages/files/@acme/scene.charts/dist/index.js");
        _style = await _client.GetAsync("/stage/packages/files/@acme/scene.charts/styles.css");
        _unapproved = await _client.GetAsync("/stage/packages/files/@other/package/index.js");
        _unknownType = await _client.GetAsync("/stage/packages/files/@acme/scene.charts/secret.txt");
        _missing = await _client.GetAsync("/stage/packages/files/@acme/scene.charts/dist/missing.js");
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact] void should_describe_the_approved_set() => _document.ShouldContain("\"Acme.Charts\"");
    [Fact] void should_say_where_the_module_is_served() => _document.ShouldContain("stage/packages/files/@acme/scene.charts/dist/index.js");
    [Fact] void should_serve_the_module() => _module.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_module_as_javascript() => _module.Content.Headers.ContentType!.MediaType.ShouldEqual("text/javascript");
    [Fact] void should_not_cache_the_module() => _module.Headers.CacheControl!.NoStore.ShouldBeTrue();
    [Fact] void should_serve_the_style_sheet() => _style.Content.Headers.ContentType!.MediaType.ShouldEqual("text/css");
    [Fact] void should_hide_a_package_the_set_does_not_approve() => _unapproved.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_hide_a_file_type_a_package_does_not_ship() => _unknownType.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_fall_back_to_a_page_for_a_missing_file() => _missing.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
