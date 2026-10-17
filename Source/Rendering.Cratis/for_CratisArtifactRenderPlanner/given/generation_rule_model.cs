// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class generation_rule_model
{
    internal const string PortableBody = "return $\"{context.Tenant}:order_{context.Now:yyyyMMdd}-{Guid.NewGuid():N}\";";
    internal const string TargetBody = "return $\"{context.Tenant}:{GenerateValue.Ulid(context)}\";";

    internal static string Source(string body, string rule = "TenantOrder", string validation = "", bool pinned = true) => $$"""
        concept TenantOrderId : String
          generate rule {{rule}}
            ```csharp
            {{body}}
            ```
        {{validation}}
        module Shop
          feature Orders
            slice StateChange Create
              command Create
                streamId String identifier
                id TenantOrderId generated
                produces Created
                  for streamId
                  {{(pinned ? "id = id" : "order = streamId")}}
              event Created
                {{(pinned ? "id TenantOrderId" : "order String")}}
              specification CreatingAnOrder
                when Create
                  streamId = "order-1"
        {{(pinned ? "          generated id = \"tenant:fixture\"" : "")}}
                then Created
        {{(pinned ? "          id = \"tenant:fixture\"" : "          order = \"order-1\"")}}
        """;

    internal static LoadedSemanticModel Load(string body = PortableBody, string rule = "TenantOrder", string validation = "", bool pinned = true) =>
        opaque_policy_model.Load(Source(body, rule, validation, pinned));
}
#endif
