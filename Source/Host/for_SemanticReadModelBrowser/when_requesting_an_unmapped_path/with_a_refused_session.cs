// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_requesting_an_unmapped_path;

public class with_a_refused_session : a_refused_browser_page
{
    HttpResponseMessage _response = null!;

    async Task Because() => _response = await _client.GetAsync("/stage/scene");
    void Destroy() => _response.Dispose();

    [Fact] void should_preserve_not_found() => _response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
