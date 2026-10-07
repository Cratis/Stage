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
            .Line("if (name == global::Cratis.Chronicle.EventStoreNamespaceName.Default) return TenantId.Default;")
            .Line("if (name == global::Cratis.Chronicle.EventStoreNamespaceName.NotSet) return TenantId.NotSet;")
            .OpenBlock("if (name.Value.Length == 0 || (global::System.Guid.TryParse(name.Value, out var id) && id == global::System.Guid.Empty))")
            .Line("throw new global::System.InvalidOperationException(\"Chronicle namespace collides with a reserved portable tenant identifier.\");")
            .EndBlock()
            .Line("return new TenantId(name.Value);")
            .EndBlock()
            .EndBlock();
        return new(Path.Combine("TypedContexts", "TenantId.cs"), builder.ToString());
    }
}
