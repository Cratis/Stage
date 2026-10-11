// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_opening_the_page;

public class without_a_trailing_slash : a_browser_page
{
    HttpResponseMessage _response = null!;
    string _body = null!;

    async Task Because()
    {
        _response = await _client.GetAsync("/read-models");
        _body = await _response.Content.ReadAsStringAsync();
    }
    void Destroy() => _response.Dispose();

    [Fact] void should_serve_the_page_without_a_redirect_loop() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_reach_the_spa_fallback() => _body.ShouldEqual("SPA fallback");
}
