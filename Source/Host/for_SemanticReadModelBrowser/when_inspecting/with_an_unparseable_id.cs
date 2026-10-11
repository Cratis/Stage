// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_inspecting;

public class with_an_unparseable_id : a_browser
{
    HttpResponseMessage _response = null!;

    async Task Because() => _response = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/invalid");
    void Destroy() => _response.Dispose();

    [Fact] void should_return_not_found() => ((int)_response.StatusCode).ShouldEqual(404);
}
