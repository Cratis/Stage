// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Emits the reducer-only portable runtime token without referencing the Screenplay compiler at runtime.</summary>
internal static class SemanticReducerContextRuntime
{
    internal static RenderedFile Render(SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.TypedContexts")
            .OpenBlock("public record TenantId(string Value)")
            .Line("public static readonly TenantId Default = new(\"00000000-0000-0000-0000-000000000000\");")
            .Line("public static readonly TenantId NotSet = new(string.Empty);")
            .Line("public static implicit operator string(TenantId tenant) => tenant.Value;")
            .Line("public static implicit operator TenantId(string value) => new(value);")
            .EndBlock()
            .BlankLine()
            .OpenBlock("internal static class ReducerContextValues")
            .OpenBlock("public static TenantId Tenant(global::Cratis.Chronicle.EventStoreNamespaceName name)")
            .Line($"return new TenantId(global::{context.RootNamespace}.GeneratedTenancy.PortableTenantValues.Translate(name.Value, global::Cratis.Chronicle.EventStoreNamespaceName.Default.Value, global::Cratis.Chronicle.EventStoreNamespaceName.NotSet.Value));")
            .EndBlock()
            .EndBlock();
        return new(Path.Combine("TypedContexts", "TenantId.cs"), builder.ToString());
    }
}
