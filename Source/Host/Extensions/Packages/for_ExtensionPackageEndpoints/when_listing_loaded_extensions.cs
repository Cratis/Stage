// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageEndpoints;

public class when_listing_loaded_extensions : a_package_front
{
    JsonElement _package;
    JsonElement _list;
    HttpResponseMessage _icon = null!;
    HttpResponseMessage _image = null!;
    string _page = null!;

    async Task Because()
    {
        await StartFront(includeRefusal: true);
        _list = JsonDocument.Parse(await _client.GetStringAsync("/stage/extensions")).RootElement;
        _package = _list.GetProperty("packages")[0];
        _icon = await Get(_package.GetProperty("icon").GetString()!);
        _image = await Get(_package.GetProperty("images")[0].GetProperty("url").GetString()!);
        _page = await _client.GetStringAsync("/stage/extensions/view");
    }

    async Task Destroy() => await StopFront();

    [Fact] void should_list_the_display_name() => _package.GetProperty("displayName").GetString().ShouldEqual("Transactional mail");
    [Fact] void should_list_the_description() => _package.GetProperty("description").GetString().ShouldEqual("Sends mail");
    [Fact] void should_list_the_locked_digest_and_publisher() => (_package.GetProperty("digest").GetString() == _entry.Digest && _package.GetProperty("publisher").GetString() == PackageFixtures.Publisher).ShouldBeTrue();
    [Fact] void should_say_the_signature_was_verified() => _package.GetProperty("signed").GetBoolean().ShouldBeTrue();
    [Fact] void should_list_the_image_captions() => _package.GetProperty("images")[0].GetProperty("caption").GetString().ShouldEqual("The inbox");
    [Fact] void should_list_the_components_with_their_placeholders() => _package.GetProperty("contents").GetProperty("components")[0].GetProperty("placeholder").GetString()!.ShouldContain("/placeholders/MailPreview");
    [Fact] void should_list_requirements() => _package.GetProperty("requirements")[0].GetProperty("kind").GetString().ShouldEqual("stage");
    [Fact] void should_list_settings_by_exposure_and_never_by_value() => _package.GetProperty("contents").GetProperty("settings")[0].GetProperty("exposure").GetString().ShouldEqual("secret");
    [Fact] void should_list_what_was_refused() => _list.GetProperty("refused")[0].GetProperty("reason").GetString().ShouldEqual("NotAllowed");
    [Fact] void should_serve_the_icon() => _icon.Content.Headers.ContentType!.MediaType.ShouldEqual("image/svg+xml");
    [Fact] void should_serve_images_that_cannot_run_script() => _icon.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("sandbox");
    [Fact] void should_serve_the_images() => _image.Content.Headers.ContentType!.MediaType.ShouldEqual("image/png");
    [Fact] void should_render_a_view_with_the_name() => _page.ShouldContain("Transactional mail");
    [Fact] void should_encode_what_a_refusal_says() => _page.ShouldContain("not allowed &lt;here&gt;");
    [Fact] void should_have_no_script_in_the_view() => _page.ShouldNotContain("<script");
}
