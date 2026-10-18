// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_locally_unadmitted_projection : a_browser
{
    protected override string ModelSource => base.ModelSource
        .Replace("event ProjectRegistered", "event ProjectRemoved\n        projectId ProjectId\n      event ProjectRegistered", StringComparison.Ordinal)
        .Replace("from ProjectRegistered key $eventSourceId", "remove via join on ProjectRemoved key projectId\n        from ProjectRegistered key $eventSourceId", StringComparison.Ordinal);

    JsonElement _list;
    HttpResponseMessage _detail = null!;
    int _queryStatus;
    string _detailBody = null!;
    string _queryBody = null!;
    JsonElement RefusedModel => _list.GetProperty("readModels")[0];

    async Task Because()
    {
        _list = await Json(SemanticReadModelBrowser.Route);
        _detail = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/{ModelId}");
        _detailBody = await _detail.Content.ReadAsStringAsync();
        var query = await Request("GET", "/api/projects/registration/project-lookup/all-project-summaries");
        _queryStatus = query.Status;
        _queryBody = query.Body;
    }

    void Destroy() => _detail.Dispose();

    [Fact] void should_keep_supported_models_browsable() => _list.GetProperty("readModels")[1].GetProperty("instances").GetInt32().ShouldEqual(0);
    [Fact] void should_not_present_an_unsupported_model_as_empty() => RefusedModel.GetProperty("instances").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_identify_the_refused_model() => RefusedModel.GetProperty("unsupported").GetProperty("artifact").GetString().ShouldEqual(ModelId.ToString());
    [Fact] void should_include_the_typed_refusal_in_the_list() => RefusedModel.GetProperty("unsupported").GetProperty("details").GetString()!.ShouldContain("STAGE-PLAN-004:");
    [Fact] void should_refuse_details() => ((int)_detail.StatusCode).ShouldEqual(501);
    [Fact] void should_return_the_projection_capability() => _detail.Headers.GetValues("Stage-Unsupported-Capability").Single().ShouldEqual("Projection");
    [Fact] void should_return_the_model_identity() => _detail.Headers.GetValues("Stage-Unsupported-Artifact").Single().ShouldEqual(ModelId.ToString());
    [Fact] void should_return_the_typed_detail_refusal() => _detailBody.ShouldContain("STAGE-PLAN-004:");
    [Fact] void should_refuse_a_conventional_snapshot_query() => _queryStatus.ShouldEqual(501);
    [Fact] void should_return_the_typed_snapshot_refusal() => _queryBody.ShouldContain("STAGE-PLAN-004:");
}
