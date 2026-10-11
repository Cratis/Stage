// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_live_instances : a_browser
{
    JsonElement _list;

    async Task Establish() => await Register();
    async Task Because() => _list = await Json(SemanticReadModelBrowser.Route);

    [Fact] void should_list_every_read_model_in_declaration_order() => _list.GetProperty("readModels").EnumerateArray().Select(model => model.GetProperty("name").GetString()).ShouldEqual(["ProjectSummary", "EmptySummary"]);
    [Fact] void should_include_empty_models_without_queries() => _list.GetProperty("readModels")[1].GetProperty("instances").GetInt32().ShouldEqual(0);
    [Fact] void should_count_the_current_instances() => _list.GetProperty("readModels")[0].GetProperty("instances").GetInt32().ShouldEqual(1);
    [Fact] void should_report_the_semantic_identity() => _list.GetProperty("readModels")[0].GetProperty("id").GetString().ShouldEqual(ModelId.ToString());
    [Fact] void should_report_the_module() => _list.GetProperty("readModels")[1].GetProperty("module").GetString().ShouldEqual("Projects");
    [Fact] void should_report_nested_features() => _list.GetProperty("readModels")[1].GetProperty("feature").GetString().ShouldEqual("Registration/History");
    [Fact] void should_report_the_slice() => _list.GetProperty("readModels")[1].GetProperty("slice").GetString().ShouldEqual("EmptyLookup");
    [Fact] void should_report_named_optional_types() => _list.GetProperty("readModels")[1].GetProperty("properties")[0].GetProperty("type").GetString().ShouldEqual("ProjectName?");
    [Fact] void should_report_collection_types() => _list.GetProperty("readModels")[0].GetProperty("properties")[2].GetProperty("type").GetString().ShouldEqual("Text[]");
}
