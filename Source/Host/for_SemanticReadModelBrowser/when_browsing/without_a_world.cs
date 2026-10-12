// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class without_a_world : a_browser
{
    HttpResponseMessage _list = null!;
    HttpResponseMessage _detail = null!;

    void Establish() => _ready = false;
    async Task Because()
    {
        _list = await _client.GetAsync(SemanticReadModelBrowser.Route);
        _detail = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/{ModelId}");
    }
    void Destroy()
    {
        _list.Dispose();
        _detail.Dispose();
    }

    [Fact] void should_report_the_list_as_unavailable() => ((int)_list.StatusCode).ShouldEqual(503);
    [Fact] void should_report_the_detail_as_unavailable() => ((int)_detail.StatusCode).ShouldEqual(503);
    [Fact] void should_offer_a_list_retry() => _list.Headers.RetryAfter!.Delta.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_offer_a_detail_retry() => _detail.Headers.RetryAfter!.Delta.ShouldEqual(TimeSpan.FromSeconds(1));
}
