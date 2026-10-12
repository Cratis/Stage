// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_opening_the_page;

public class with_a_query_string : a_browser_page
{
    HttpResponseMessage _response = null!;

    async Task Because() => _response = await _client.GetAsync("/read-models/?model=Project%20Summary&refresh=true");
    void Destroy() => _response.Dispose();

    [Fact] void should_redirect_before_serving_the_page() => _response.StatusCode.ShouldEqual(HttpStatusCode.Found);
    [Fact] void should_preserve_the_query_string() => _response.Headers.Location!.OriginalString.ShouldEqual("../read-models?model=Project%20Summary&refresh=true");
}
