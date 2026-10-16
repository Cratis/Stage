// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The file-wide imports and visible application declarations shared by rendering and closed reducer analysis.
/// </summary>
/// <param name="Imports">The exact file-wide using namespaces.</param>
/// <param name="Declarations">Imported application declarations and the closed analyzer's common declaration inventory.</param>
/// <param name="Events">Visible event contracts whose immutable shapes are needed for analysis.</param>
internal sealed record SemanticReducerBindingEnvironment(string[] Imports, GeneratedTypeDeclaration[] Declarations, SemanticEventContract[] Events)
{
    internal static SemanticReducerBindingEnvironment Create(LocatedSemanticSlice located, SemanticApplicationContext context, SemanticReducer? classicReducer)
    {
        var builder = new CSharpCodeBuilder();
        IEnumerable<SemanticReducer> reducers;
        if (classicReducer is not null)
        {
            // A classic reducer has its own file. Preserve its historical imports exactly.
            builder.Using("Cratis.Chronicle.Events")
                .Using("Cratis.Chronicle.Reducers")
                .Using($"{context.RootNamespace}.TypedContexts")
                .Using(SliceNaming.Namespace(context.RootNamespace, located.Path));
            reducers = [classicReducer];
        }
        else
        {
            if (located.Slice.Kind == SemanticSliceKind.Automation)
            {
                SemanticStateChangeArtifactRenderer.Configure(builder, located, context);
            }
            SemanticStateViewArtifactRenderer.ConfigureImports(builder, located, context);
            reducers = context.SelfReducing ? located.Slice.Reducers : [];
        }
        foreach (var transition in reducers.SelectMany(reducer => reducer.Transitions))
        {
            builder.Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(transition.EventContract).Path));
        }

        var imports = builder.UsingNamespaces.Order(StringComparer.Ordinal).ToArray();
        var namespaces = imports.Append(SliceNaming.Namespace(context.RootNamespace, located.Path)).ToHashSet(StringComparer.Ordinal);

        // Common definitions are compilation peers, never extra imports. Retain the historical
        // fail-closed shadow inventory even when a classic file does not import Common.
        var declarations = SemanticCratisAdmission.GeneratedDeclarations(context, context.AllSlices)
            .Where(declaration => declaration.Kind != "Generated" &&
                (namespaces.Contains(Qualify(context, declaration.Namespace)) || declaration.Kind == "Concept" || declaration.Kind == "Type"))
            .ToArray();
        var events = context.Events.Values.Where(@event => namespaces.Contains(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path))).ToArray();
        return new(imports, declarations, events);
    }

    internal void Configure(CSharpCodeBuilder builder)
    {
        foreach (var import in Imports) builder.Using(import);
    }

    internal static string Qualify(SemanticApplicationContext context, string relativeNamespace) =>
        relativeNamespace.Length == 0 ? context.RootNamespace : $"{context.RootNamespace}.{relativeNamespace}";
}
