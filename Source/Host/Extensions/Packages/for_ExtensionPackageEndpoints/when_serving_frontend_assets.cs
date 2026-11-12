// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageEndpoints;

public class when_serving_frontend_assets : a_package_front
{
    HttpResponseMessage _entry = null!;
    HttpResponseMessage _style = null!;
    HttpResponseMessage _readme = null!;
    HttpResponseMessage _contract = null!;
    HttpResponseMessage _traversal = null!;
    HttpResponseMessage _unknownPackage = null!;

    async Task Because()
    {
        await StartFront();
        _entry = await Get($"/extension-assets/{_id}/index.js");
        _style = await Get($"/extension-assets/{_id}/style.css");
        _readme = await Get($"/extension-assets/{_id}/../README.md");
        _contract = await Get($"/extension-assets/{_id}/%2e%2e/contract.play");
        _traversal = await Get($"/extension-assets/{_id}/../../contract.play");
        _unknownPackage = await Get("/extension-assets/" + new string('0', 64) + "/index.js");
    }

    async Task Destroy() => await StopFront();

    [Fact] void should_serve_the_frontend_entry() => _entry.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_it_as_script() => _entry.Content.Headers.ContentType!.MediaType.ShouldEqual("text/javascript");
    [Fact] void should_serve_the_rest_of_the_frontend_folder() => _style.Content.Headers.ContentType!.MediaType.ShouldEqual("text/css");
    [Fact] void should_not_serve_files_outside_the_frontend_folder() => _readme.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_the_contract() => _contract.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_follow_a_path_out_of_the_package() => _traversal.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_find_a_package_that_is_not_loaded() => _unknownPackage.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_name_the_digest_in_the_etag() => _entry.Headers.ETag!.Tag.ShouldContain(_id);
    [Fact] void should_let_the_response_be_cached_forever_because_the_address_names_the_digest() => _entry.Headers.CacheControl!.ToString().ShouldContain("immutable");
}
