// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_the_eventmodel_engine : a_browser
{
    protected override string Engine => "eventmodel";
    HttpResponseMessage _list = null!;
    HttpResponseMessage _detail = null!;
    JsonElement _body;

    async Task Because()
    {
        _list = await _client.GetAsync(SemanticReadModelBrowser.Route);
        _detail = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/{ModelId}");
        using var document = JsonDocument.Parse(await _detail.Content.ReadAsStringAsync());
        _body = document.RootElement.Clone();
    }
    void Destroy()
    {
        _list.Dispose();
        _detail.Dispose();
    }

    [Fact] void should_not_serve_the_spa_for_the_list() => ((int)_list.StatusCode).ShouldEqual(404);
    [Fact] void should_not_serve_the_spa_for_the_detail() => ((int)_detail.StatusCode).ShouldEqual(404);
    [Fact] void should_report_the_engine() => _body.GetProperty("engine").GetString().ShouldEqual("eventmodel");
    [Fact] void should_direct_the_reader_to_the_workbench() => _body.GetProperty("reason").GetString()!.Contains("Chronicle Workbench", StringComparison.Ordinal).ShouldBeTrue();
}
