// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_refused_model : a_browser
{
    protected override bool Refused => true;
    HttpResponseMessage _list = null!;
    HttpResponseMessage _detail = null!;
    string _body = null!;

    async Task Because()
    {
        _list = await _client.GetAsync(SemanticReadModelBrowser.Route);
        _detail = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/{ModelId}");
        _body = await _detail.Content.ReadAsStringAsync();
    }
    void Destroy()
    {
        _list.Dispose();
        _detail.Dispose();
    }

    [Fact] void should_refuse_the_list() => ((int)_list.StatusCode).ShouldEqual(501);
    [Fact] void should_refuse_the_detail() => ((int)_detail.StatusCode).ShouldEqual(501);
    [Fact] void should_return_the_specification_refusal_shape() => _body.ShouldContain("Unsupported(Plan)");
    [Fact] void should_return_the_refusal_header() => _detail.Headers.GetValues("Stage-Unsupported-Capability").Single().ShouldEqual("Plan");
}
