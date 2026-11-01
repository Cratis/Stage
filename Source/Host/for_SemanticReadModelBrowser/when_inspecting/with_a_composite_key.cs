// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_inspecting;

public class with_a_composite_key : a_browser
{
    JsonElement _detail;
    protected override string ModelSource => """
        concept ProjectId : Uuid
        type Address
          city String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                address Address
                produces ProjectRegistered
                  for projectId
                  address = address
              event ProjectRegistered
                address Address
            slice StateView ProjectLookup
              readmodel ProjectSummary
                address Address key
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key address
                  address = address
        """;

    async Task Establish()
    {
        new SemanticRuntimeAdmission(_semanticRuntime!.Plan).Blocking.ShouldBeEmpty();
        await Register("""
            {"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","address":{"city":"Oslo"}}
            """);
    }
    async Task Because() => _detail = await Json($"{SemanticReadModelBrowser.Route}/{ModelId}");

    [Fact] void should_report_the_composite_key_as_an_object() => _detail.GetProperty("instances")[0].GetProperty("key").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_resolve_the_key_property_names() => _detail.GetProperty("instances")[0].GetProperty("key").GetProperty("city").GetString().ShouldEqual("Oslo");
}
