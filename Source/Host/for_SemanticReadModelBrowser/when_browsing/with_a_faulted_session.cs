// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_faulted_session : a_browser
{
    HttpResponseMessage _list = null!;
    HttpResponseMessage _detail = null!;
    JsonElement _body;

    async Task Establish()
    {
        await Register();
        _failAppend = true;
        using var response = await _client.PostAsync(CommandRoute, new StringContent(Payload.Replace("Screenplay", "Another", StringComparison.Ordinal), Encoding.UTF8, "application/json"));
    }

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

    [Fact] void should_refuse_the_list() => ((int)_list.StatusCode).ShouldEqual(503);
    [Fact] void should_refuse_the_detail() => ((int)_detail.StatusCode).ShouldEqual(503);
    [Fact] void should_report_the_fault() => _body.GetProperty("state").GetString().ShouldEqual("faulted");
    [Fact] void should_include_the_fault_reason() => _body.GetProperty("reason").GetString().ShouldEqual(((ISemanticRuntimeStatus)_semanticRuntime!).FaultReason);
    [Fact] void should_not_return_stale_instances() => _body.TryGetProperty("instances", out _).ShouldBeFalse();
}
