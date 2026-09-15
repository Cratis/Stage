// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Admits only complete C# bodies proven not to bind to the generated context parameter.
/// This is a binding check, not a compilation check or a Screenplay-to-Arc context mapping.
/// </summary>
internal static class InlineCommandHandlerAdmission
{
    static readonly string[] _namespaces = ["System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks"];
    static readonly Lazy<MetadataReference[]> _references = new(() =>
        [.. new[] { typeof(object).Assembly, typeof(Enumerable).Assembly, Assembly.Load("System.Runtime") }
            .Distinct()
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))]);

    /// <summary>
    /// Rejects an inline handler unless its independence can be established.
    /// </summary>
    /// <param name="code">The authored code block.</param>
    /// <param name="commandName">The command owning the handler.</param>
    /// <param name="fullSlicePath">The full selected slice path.</param>
    /// <exception cref="UnsupportedInlineCommandHandler">The body cannot be admitted.</exception>
    public static void EnsureAccepted(CodeBlockSyntax code, string commandName, string fullSlicePath)
    {
        InlineCommandHandlerRejectionReason? reason;
        try
        {
            reason = Analyze(code);
        }
        catch (Exception exception)
        {
            throw new UnsupportedInlineCommandHandler(commandName, fullSlicePath, code.Location, InlineCommandHandlerRejectionReason.AnalysisFailure, exception);
        }

        if (reason is not null)
        {
            throw new UnsupportedInlineCommandHandler(commandName, fullSlicePath, code.Location, reason.Value);
        }
    }

    static InlineCommandHandlerRejectionReason? Analyze(CodeBlockSyntax code)
    {
        if (!string.Equals(code.Language, "csharp", StringComparison.Ordinal))
        {
            return InlineCommandHandlerRejectionReason.UnsupportedLanguage;
        }

        // Parse the entire input as ONE block, not as interpolated members of a compilation unit.
        // consumeFullText retains trailing/skipped tokens; the closing brace must be ours, not authored.
        var text = "{\n" + code.Code + "\n}";
        var statement = SyntaxFactory.ParseStatement(text, options: new CSharpParseOptions(), consumeFullText: true);
        if (statement.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective))
        {
            return InlineCommandHandlerRejectionReason.Directives;
        }

        if (statement is not BlockSyntax body || body.ContainsDiagnostics || body.ContainsSkippedText ||
            body.CloseBraceToken.IsMissing || body.CloseBraceToken.SpanStart != text.Length - 1)
        {
            return InlineCommandHandlerRejectionReason.MalformedBody;
        }

        // The parameter needs an identity, not either framework's context type. No member mapping is assumed.
        var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("IEnumerable<object>"), "Handle")
            .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
            .WithBody(body);
        var root = SyntaxFactory.CompilationUnit()
            .AddUsings([.. _namespaces.Select(name => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name)))])
            .AddMembers(SyntaxFactory.ClassDeclaration("InlineHandler").AddMembers(method));
        var tree = CSharpSyntaxTree.Create(root);
        var compilation = CSharpCompilation.Create(
            "InlineHandlerAdmission", [tree], _references.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var attachedMethod = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var parameter = model.GetDeclaredSymbol(attachedMethod.ParameterList.Parameters.Single());
        if (parameter is null)
        {
            return InlineCommandHandlerRejectionReason.AnalysisFailure;
        }

        foreach (var name in attachedMethod.Body!.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            var binding = model.GetSymbolInfo(name);
            if (SymbolEqualityComparer.Default.Equals(binding.Symbol, parameter) ||
                binding.CandidateSymbols.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, parameter)))
            {
                return InlineCommandHandlerRejectionReason.ContextBinding;
            }

            // Unrelated unresolved generated types are outside this check. A context name, however, must
            // conclusively resolve elsewhere (e.g. a shadowing lambda parameter or an anonymous property).
            if (name.Identifier.ValueText == "context" &&
                (binding.Symbol is null or ITypeSymbol { TypeKind: TypeKind.Error } || !binding.CandidateSymbols.IsEmpty))
            {
                return InlineCommandHandlerRejectionReason.UncertainContextBinding;
            }
        }

        return null;
    }
}
