// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageEndpoints;

public class when_serving_a_placeholder : a_package_front
{
    HttpResponseMessage _frame = null!;
    HttpResponseMessage _navigation = null!;
    HttpResponseMessage _unknown = null!;
    string _html = null!;
    string _policy = null!;

    async Task Because()
    {
        await StartFront();
        var path = $"/stage/extensions/packages/{_id}/placeholders/MailPreview";
        _frame = await Get(path, "iframe");
        _html = await _frame.Content.ReadAsStringAsync();
        _policy = _frame.Headers.GetValues("Content-Security-Policy").Single();
        _navigation = await Get(path, "document");
        _unknown = await Get($"/stage/extensions/packages/{_id}/placeholders/Nothing", "iframe");
    }

    async Task Destroy() => await StopFront();

    [Fact] void should_serve_it_into_a_frame() => _frame.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_what_the_package_shipped() => _html.ShouldEqual(PackageFixtures.Placeholder);
    [Fact] void should_forbid_everything_by_default() => _policy.ShouldContain("default-src 'none'");
    [Fact] void should_allow_no_script_at_all() => _policy.ShouldNotContain("script-src");
    [Fact] void should_load_nothing_from_outside() => _policy.ShouldNotContain("http");
    [Fact] void should_only_allow_inline_styles_and_data_images() => _policy.ShouldContain("style-src 'unsafe-inline'; img-src data:");
    [Fact] void should_sandbox_the_document_even_when_it_is_opened_on_its_own() => _policy.Split("; ").ShouldContain("sandbox");
    [Fact] void should_not_let_it_be_framed_by_another_origin() => _policy.ShouldContain("frame-ancestors 'self'");
    [Fact] void should_forbid_content_sniffing() => _frame.Headers.GetValues("X-Content-Type-Options").Single().ShouldEqual("nosniff");
    [Fact] void should_refuse_to_show_it_as_a_page() => _navigation.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_find_a_component_that_has_no_placeholder() => _unknown.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
