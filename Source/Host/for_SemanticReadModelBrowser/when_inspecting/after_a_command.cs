// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_inspecting;

public class after_a_command : a_browser
{
    JsonElement _detail;
    JsonElement Instance => _detail.GetProperty("instances")[0];
    JsonElement Values => Instance.GetProperty("values");

    async Task Establish() => await Register();
    async Task Because() => _detail = await Json($"{SemanticReadModelBrowser.Route}/{ModelId}");

    [Fact] void should_use_the_current_world() => _detail.GetProperty("instances").GetArrayLength().ShouldEqual(1);
    [Fact] void should_report_the_model_name() => _detail.GetProperty("name").GetString().ShouldEqual("ProjectSummary");
    [Fact] void should_report_the_key_as_json() => Instance.GetProperty("key").GetString().ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_use_property_names_for_values() => Values.GetProperty("name").GetString().ShouldEqual("Screenplay");
    [Fact] void should_preserve_arrays() => Values.GetProperty("tags").EnumerateArray().Select(value => value.GetString()).ShouldEqual(["one", "two"]);
    [Fact] void should_preserve_booleans() => Values.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_numbers() => Values.GetProperty("count").GetInt32().ShouldEqual(7);
    [Fact] void should_use_composite_property_names() => Values.GetProperty("address").GetProperty("city").GetString().ShouldEqual("Oslo");
    [Fact] void should_report_property_metadata() => _detail.GetProperty("properties").GetArrayLength().ShouldEqual(6);
}
