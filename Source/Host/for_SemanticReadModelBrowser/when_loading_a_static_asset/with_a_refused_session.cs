// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_loading_a_static_asset;

public class with_a_refused_session : a_refused_browser_page
{
    HttpResponseMessage _response = null!;
    string _body = null!;

    async Task Because()
    {
        _response = await _client.GetAsync("/assets/browser.js");
        _body = await _response.Content.ReadAsStringAsync();
    }
    void Destroy() => _response.Dispose();

    [Fact] void should_serve_the_browser_asset() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_the_asset_content() => _body.ShouldEqual("console.log('browser');");
}
