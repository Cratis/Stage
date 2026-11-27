// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Emits authenticated provided-value bodies with their portable Screenplay context.</summary>
internal static class SemanticProvidedBodyRendering
{
    internal static string Name(SemanticProvidedValue value) => "StageProvide_" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(((SemanticCodeProvideSource)value.Source).Implementation + ":" + value.Name)))[..16];

    internal static string ResultType(SemanticProvidedValue value, SemanticApplicationContext context)
    {
        var type = value.ReadModel is { } id
            ? $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(id).Path)}.{Identifiers.ToPascalCase(context.ReadModels[id].Name)}"
            : new SemanticTypeSystem(context).Type(value.Type!);
        return type.EndsWith('?') ? type : type + "?";
    }

    internal static void RenderDeclared(CSharpCodeBuilder builder, IReadOnlyList<SemanticProvidedDeclaration> declarations, SemanticApplicationContext context)
    {
        var bodies = declarations.Where(declaration => declaration.Value.Source is SemanticCodeProvideSource).ToArray();
        foreach (var declaration in bodies)
        {
            var errors = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
            if (!SemanticProvidedBodyAdmission.TryBody(context, declaration, errors, out var body))
                throw new InvalidOperationException(errors[0].Message);
            Render(builder, declaration.Value, context, body!);
        }
        if (bodies.Length > 0) SemanticProvidedContextRendering.Render(builder, context);
    }

    internal static void Render(CSharpCodeBuilder builder, SemanticProvidedValue value, SemanticApplicationContext context, string body)
    {
        builder.OpenBlock($"static class {Name(value)}")
            .OpenBlock($"public static {ResultType(value, context)} Provide(global::Cratis.Screenplay.Contexts.ProvideContext context)")
            .RawVerbatim(body, ((SemanticCodeProvideSource)value.Source).Implementation)
            .EndBlock()
            .EndBlock().BlankLine();
    }
}
