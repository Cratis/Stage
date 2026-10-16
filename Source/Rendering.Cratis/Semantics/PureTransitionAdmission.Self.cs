// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class PureTransitionAdmission
{
    // Reuse the actual record, query signatures, imports and transition members. Only boundary
    // implementations and attributes are stubbed; query/helper names still participate in C# binding.
    static string SelfAnalysisCode(SemanticApplicationContext context, LocatedSemanticSlice located, SemanticReadModel model, SemanticReducer reducer, SemanticTypedContextDescriptor descriptor, string body)
    {
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.ReducerBindings(located, reducer).Configure(builder);
        var queries = located.Slice.Queries.Where(query => query.ReadModel == model.Id).ToArray();
        var types = new SemanticTypeSystem(context);
        builder.OpenBlock(SemanticStateViewArtifactRenderer.ModelDeclaration(model, types, queries, true));
        foreach (var query in queries)
        {
            SemanticStateViewArtifactRenderer.RenderQuery(builder, query, model, types, context);
        }
        SemanticReducerArtifactRenderer.RenderTransitions(builder, reducer, context, descriptor.RequirementId, body);
        builder.EndBlock();
        var root = CSharpSyntaxTree.ParseText(builder.ToString()).GetRoot();
        var queriesSyntax = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(method =>
            !method.Identifier.Text.StartsWith("Transition_", StringComparison.Ordinal) && method.Identifier.Text != "Reduce").ToArray();
        root = root.ReplaceNodes(queriesSyntax, (original, _) => original
            .WithModifiers(SyntaxFactory.TokenList(original.Modifiers.Where(token => !token.IsKind(SyntaxKind.AsyncKeyword))))
            .WithExpressionBody(null).WithSemicolonToken(default)
            .WithBody((BlockSyntax)SyntaxFactory.ParseStatement("{ throw new global::System.NotImplementedException(); }")));
        root = root.RemoveNodes(root.DescendantNodes().OfType<AttributeListSyntax>(), SyntaxRemoveOptions.KeepNoTrivia)!;
        return root.ToFullString();
    }
}
