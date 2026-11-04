// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Restores the hashed typed-context and reducer helper names Stage emitted before Cratis/Stage#850 made them
/// readable, so an earlier frozen digest remains a gate proving that the names were the only change.
/// </summary>
internal static class HashedHelperNames
{
    const string SelfTransition = "Apply";

    /// <summary>Restores the hashed spelling in rendered text and in a rendered relative path.</summary>
    /// <param name="text">The rendered text or path.</param>
    /// <param name="context">The semantic application the text was rendered from.</param>
    /// <returns>The text as the previous renderer spelled it.</returns>
    internal static string Restore(string text, SemanticApplicationContext context) => RestoreSyntax(text, Entries(context));

    /// <summary>
    /// Restores generated declaration and reference identifiers by syntax. Only identifier tokens outside the authored
    /// regions are rewritten; comments, every string literal and the authored bodies keep their exact bytes.
    /// </summary>
    /// <param name="text">The rendered C# source.</param>
    /// <param name="entries">The generated helper names and the authored regions they own.</param>
    /// <returns>The source as the previous renderer spelled it.</returns>
    /// <exception cref="InvalidOperationException">A generated name maps to more than one previous name.</exception>
    internal static string RestoreSyntax(string text, IReadOnlyList<Entry> entries)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (names.TryGetValue(entry.Current, out var existing) && existing != entry.Previous)
            {
                throw new InvalidOperationException($"Generated name '{entry.Current}' maps to both '{existing}' and '{entry.Previous}'.");
            }

            names[entry.Current] = entry.Previous;
        }

        var bodyMethods = entries.Where(entry => entry.Protection == Protection.Body).Select(entry => entry.Current).ToHashSet(StringComparer.Ordinal);
        var checkedMethods = entries.Where(entry => entry.Protection == Protection.Checked).Select(entry => entry.Current).ToHashSet(StringComparer.Ordinal);
        var root = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)).GetRoot();
        var protectedSpans = ProtectedSpans(root, bodyMethods, checkedMethods).ToArray();
        var edits = root.DescendantTokens()
            .Where(token => token.IsKind(SyntaxKind.IdentifierToken) && names.ContainsKey(token.ValueText) && token.Text == token.ValueText && !protectedSpans.Any(span => span.Contains(token.Span)))
            .OrderByDescending(token => token.SpanStart)
            .ToArray();
        var result = text;
        foreach (var token in edits)
        {
            result = string.Concat(result.AsSpan(0, token.SpanStart), names[token.ValueText], result.AsSpan(token.Span.End));
        }

        return result;
    }

    static IEnumerable<Microsoft.CodeAnalysis.Text.TextSpan> ProtectedSpans(SyntaxNode root, HashSet<string> bodyMethods, HashSet<string> checkedMethods) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Body is not null)
            .SelectMany(method => MethodSpans(method, bodyMethods, checkedMethods));

    static IEnumerable<Microsoft.CodeAnalysis.Text.TextSpan> MethodSpans(MethodDeclarationSyntax method, HashSet<string> bodyMethods, HashSet<string> checkedMethods)
    {
        var name = method.Identifier.ValueText;
        if (bodyMethods.Contains(name) || name == SelfTransition) return [method.Body!.Span];
        if (!checkedMethods.Contains(name)) return [];
        return method.Body!.DescendantNodes().OfType<CheckedStatementSyntax>().Select(block => block.Block.Span);
    }

    /// <summary>Restores every rendered artifact's path and content.</summary>
    /// <param name="artifacts">The rendered artifacts.</param>
    /// <param name="context">The semantic application the artifacts were rendered from.</param>
    /// <returns>The artifacts as the previous renderer spelled them.</returns>
    internal static IEnumerable<global::Cratis.Stage.Contracts.Rendering.PlannedArtifact> Restore(IEnumerable<global::Cratis.Stage.Contracts.Rendering.PlannedArtifact> artifacts, SemanticApplicationContext context) =>
        artifacts.Select(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)
            ? global::Cratis.Stage.Contracts.Rendering.PlannedArtifact.CreateText(
                RestorePath(artifact.RelativePath, context),
                Restore(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), context),
                artifact.Sources)
            : artifact);

    static string RestorePath(string path, SemanticApplicationContext context)
    {
        var directory = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
        var file = Path.GetFileName(path);
        foreach (var descriptor in Descriptors(context))
        {
            var suffix = Suffix(descriptor);
            if (file == context.ContextNames.ContextPolicyFile(descriptor)) file = $"PolicyBodies_{suffix}.cs";
            else if (file == $"{context.ContextNames.ContextWrapper(descriptor)}.cs") file = $"TypedContext_{suffix}.cs";
        }

        return directory.Length == 0 ? file : $"{directory}/{file}";
    }

    static IReadOnlyList<Entry> Entries(SemanticApplicationContext context)
    {
        var entries = new List<Entry>();
        foreach (var descriptor in Descriptors(context))
        {
            var names = context.ContextNames;
            var suffix = Suffix(descriptor);
            var wrapper = $"TypedContext_{suffix}";
            entries.Add(new(names.ContextWrapper(descriptor), wrapper, Protection.None));
            foreach (var member in descriptor.Members.IsDefault ? [] : descriptor.Members)
            {
                entries.Add(new(names.ContextShape(descriptor, member.Name), $"{wrapper}_{Identifiers.ToPascalCase(member.Name)}Shape", Protection.None));
            }

            switch (descriptor.Role)
            {
                case SemanticImplementationRole.ReducerTransition:
                    entries.Add(new(context.InlineNames.ReducerTransition(descriptor.RequirementId), "Transition_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(descriptor.RequirementId)))[..8], Protection.Body));
                    break;
                case SemanticImplementationRole.PolicyPredicate:
                    entries.Add(new(names.ContextEvaluate(descriptor), $"Evaluate_{suffix}", Protection.None));
                    entries.Add(new(names.ContextMethod(descriptor), $"Body_{suffix}", Protection.Body));
                    break;
                default:
                    var iterator = SemanticValidationContextRuntime.IsIterator(descriptor);
                    entries.Add(new(names.ContextCore(descriptor), $"{(iterator ? "Validate" : "Rule")}_{suffix}_Core", Protection.Checked));
                    entries.Add(new(names.ContextMethod(descriptor), $"{(iterator ? "Validate" : "Rule")}_{suffix}", Protection.None));
                    break;
            }
        }

        return entries;
    }

    /// <summary>Which part of a generated method the authored body occupies.</summary>
    internal enum Protection
    {
        /// <summary>The declaration has no authored body.</summary>
        None,

        /// <summary>The whole method body is authored.</summary>
        Body,

        /// <summary>Only the checked blocks inside the method are authored.</summary>
        Checked
    }

    /// <summary>One generated helper name, the name the previous renderer gave it and the authored region it owns.</summary>
    /// <param name="Current">The readable name.</param>
    /// <param name="Previous">The hashed name.</param>
    /// <param name="Protection">The authored region of a method with this name.</param>
    internal sealed record Entry(string Current, string Previous, Protection Protection);

    static IEnumerable<SemanticTypedContextDescriptor> Descriptors(SemanticApplicationContext context) =>
        context.Request.TypedContextDescriptors.IsDefault ? [] : context.Request.TypedContextDescriptors.Where(descriptor => descriptor is not null);

    static string Suffix(SemanticTypedContextDescriptor descriptor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{descriptor.RequirementId}:{descriptor.OperationId}")))[..16];
}
#endif
