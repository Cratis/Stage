// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_serving_an_asset : a_model_asset_host
{
    HttpResponseMessage _logo = null!;
    HttpResponseMessage _nested = null!;
    HttpResponseMessage _head = null!;
    HttpResponseMessage _post = null!;
    string _body = null!;

    async Task Because()
    {
        _logo = await _client.GetAsync("/assets/logo.svg");
        _body = await _logo.Content.ReadAsStringAsync();
        _nested = await _client.GetAsync("/assets/nested/mark.svg");
        _head = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/assets/logo.svg"));
        _post = await _client.PostAsync("/assets/logo.svg", new StringContent(string.Empty));
    }

    [Fact] void should_serve_the_file() => _logo.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_its_content() => _body.ShouldEqual(LogoText);
    [Fact] void should_use_the_content_type_of_the_file() => _logo.Content.Headers.ContentType!.MediaType.ShouldEqual("image/svg+xml");
    [Fact] void should_not_add_cache_headers() => _logo.Headers.CacheControl.ShouldBeNull();
    [Fact] void should_serve_files_in_nested_folders() => _nested.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_answer_head() => _head.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_accept_a_write() => _post.StatusCode.ShouldNotEqual(HttpStatusCode.OK);
}
