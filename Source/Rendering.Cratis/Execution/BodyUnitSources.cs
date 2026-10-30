// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Obtains executable declarations from the renderer, never from analysis-only stand-ins.</summary>
internal static class BodyUnitSources
{
    internal static string[] Render(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string body)
    {
        var wrapper = SemanticValidationContextRuntime.RenderWrapper(context, descriptor).Content;
        var sources = new List<string>
        {
            "global using System; global using System.Collections.Generic; global using System.Linq;",
            wrapper,
            SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)]).Content
        };
        var included = new HashSet<string>(StringComparer.Ordinal);
        IncludeParameters(wrapper);
        return [.. sources];

        void IncludeParameters(string source)
        {
            var parameters = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<ParameterSyntax>();
            foreach (var name in parameters.SelectMany(parameter => parameter.Type!.DescendantNodesAndSelf().OfType<SimpleNameSyntax>()).Select(name => name.Identifier.ValueText).Distinct(StringComparer.Ordinal))
            {
                if (!included.Add(name)) continue;
                var declaration = Declaration(context, name);
                if (declaration is null) continue;
                sources.Add(declaration);
                IncludeParameters(declaration);
            }
        }
    }

    static string? Declaration(SemanticApplicationContext context, string name)
    {
        var concept = context.Application.Concepts.FirstOrDefault(concept => Identifiers.ToPascalCase(concept.Name) == name);
        if (concept is not null) return SemanticCommonArtifactRenderer.Render(concept, context, qualified: true, declarationOnly: true).Content;
        var composite = context.Application.Types.FirstOrDefault(type => Identifiers.ToPascalCase(type.Name) == name);
        if (composite is not null) return SemanticCommonArtifactRenderer.Render(composite, context, qualified: true).Content;
        var command = context.Commands.Values.FirstOrDefault(command => Identifiers.ToPascalCase(command.Name) == name);
        return command is null ? null : CommandDeclaration(context, command);
    }

    static string CommandDeclaration(SemanticApplicationContext context, SemanticCommand command)
    {
        using var qualified = QualifiedSpelling.Begin();
        var source = SemanticStateChangeArtifactRenderer.Render(context.DeclaringSlice(command.Id), context).Content;
        var syntax = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var declaration = syntax.DescendantNodes().OfType<RecordDeclarationSyntax>().Single(record => record.Identifier.ValueText == Identifiers.ToPascalCase(command.Name));
        var parameters = declaration.ParameterList!.WithParameters(SyntaxFactory.SeparatedList(
            declaration.ParameterList.Parameters.Select(parameter => parameter.WithAttributeLists(default))));

        // Retain the renderer's exact input declaration, not handler/provider interfaces or method implementations.
        var input = declaration.WithMembers(default).WithBaseList(null).WithAttributeLists(default).WithParameterList(parameters);
        var ns = syntax.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().First().Name.ToString();
        return $"namespace {ns} {{ {input} }}";
    }
}
