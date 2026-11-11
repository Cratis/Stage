// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>Frozen shared-body emission from 33693412, with an inverse relocation and exact Roslyn method-body comparison for Cratis/Stage#850.</summary>
internal static class SharedValidationBodies
{
    internal static IReadOnlyList<PlannedArtifact> Restore(IReadOnlyList<PlannedArtifact> artifacts, SemanticApplicationContext context)
    {
        var descriptors = SemanticValidationContextRuntime.Selected(context, context.AllSlices).ToArray();
        if (descriptors.Length == 0) return artifacts;
        var oldText = OldText(context, descriptors);
        var oldMethods = Methods(oldText);
        var newMethods = new Dictionary<string, string>(StringComparer.Ordinal);
        var restored = artifacts.Select(artifact => RestoreArtifact(artifact, context, descriptors, newMethods)).ToList();
        Assert.NotEmpty(oldMethods);
        Assert.Equal(oldMethods.OrderBy(pair => pair.Key, StringComparer.Ordinal), newMethods.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        restored.Add(PlannedArtifact.CreateText(Path.Combine(context.TypedContextsFolder, "ValidationBodies.cs"), oldText, []));
        return restored;
    }

    static string OldText(SemanticApplicationContext context, SemanticTypedContextDescriptor[] descriptors)
    {
        var builder = context.CodeBuilder().Namespace(context.TypedContextsNamespace);
        foreach (var owner in descriptors.Select(descriptor => descriptor.OperationId!.Value).Distinct().Where(context.Commands.ContainsKey))
            builder.Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(owner).Path));
        builder.OpenBlock("internal static class ValidationBodies");
        foreach (var descriptor in descriptors) OldBody(builder, context, descriptor);
        builder.EndBlock();
        return builder.ToString();
    }

    static void OldBody(CSharpCodeBuilder builder, SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var code = context.Request.ImplementationContents[descriptor.RequirementId];
        var iterator = SemanticValidationContextRuntime.IsIterator(descriptor);
        var result = iterator ? "global::System.Collections.Generic.IEnumerable<string>" : "bool";
        var wrapper = $"global::{context.TypedContextsNamespace}.{SemanticValidationContextRuntime.Wrapper(context, descriptor)}";
        builder.OpenBlock($"public static {result} {SemanticValidationContextRuntime.Method(context, descriptor)}({wrapper} context)");
        var implementation = iterator ? $"return global::System.Linq.Enumerable.ToArray({context.ContextNames.ContextCore(descriptor)}(context));" : code;
        RenderBoundary(builder, implementation, iterator, iterator ? null : descriptor.RequirementId);
        builder.EndBlock().BlankLine();
        if (!iterator) return;
        builder.OpenBlock($"static {result} {context.ContextNames.ContextCore(descriptor)}({wrapper} context)")
            .OpenBlock("checked").RawVerbatim(InvariantValidationInterpolation.Render(code), descriptor.RequirementId).EndBlock().EndBlock().BlankLine();
    }

    static PlannedArtifact RestoreArtifact(PlannedArtifact artifact, SemanticApplicationContext context, SemanticTypedContextDescriptor[] descriptors, Dictionary<string, string> newMethods)
    {
        if (!artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)) return artifact;
        var owners = descriptors.Select(descriptor => descriptor.OperationId!.Value).Distinct().ToArray();
        var rulesNames = owners.Select(owner => context.ContextNames.ContextRules(owner)).ToHashSet(StringComparer.Ordinal);
        var text = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        var classes = CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(type => rulesNames.Contains(type.Identifier.ValueText)).OrderByDescending(type => type.SpanStart).ToArray();
        foreach (var type in classes)
        {
            foreach (var pair in Methods(type.ToFullString())) newMethods.Add(pair.Key, pair.Value);

            // RenderOwnerBodies adds exactly one blank line before a sibling class, and the class ends the file.
            text = text.Remove(type.SpanStart - 1, type.Span.End - type.SpanStart + 2);
        }
        foreach (var owner in owners) text = RewriteReferences(text, context, owner);

        // The sibling class pulled in the globalization using for its culture boundary; without the class nothing else here needs it.
        if (classes.Length > 0 && !text.Contains("CultureInfo", StringComparison.Ordinal))
            text = text.Replace("using System.Globalization;\n", string.Empty, StringComparison.Ordinal);
        return PlannedArtifact.CreateText(artifact.RelativePath, text, artifact.Sources);
    }

    static string RewriteReferences(string text, SemanticApplicationContext context, SemanticId owner)
    {
        text = text.Replace($"global::{SemanticValidationContextRuntime.RulesType(context, owner)}.", $"global::{context.TypedContextsNamespace}.ValidationBodies.", StringComparison.Ordinal);

        // Rendered text is simplified, so the reference is the bare class name; the word boundary keeps other identifiers intact.
        return Regex.Replace(text, $@"(?<![\w.]){Regex.Escape(context.ContextNames.ContextRules(owner))}\.", "ValidationBodies.", RegexOptions.None, TimeSpan.FromSeconds(5));
    }

    static Dictionary<string, string> Methods(string text) => CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
        .ToDictionary(method => method.Identifier.ValueText, method => method.Body!.ToFullString(), StringComparer.Ordinal);

    // Unmodified RenderBoundary from 33693412: deliberately independent of current body emission.
    static void RenderBoundary(CSharpCodeBuilder builder, string body, bool iterator, string? requirementId)
    {
        builder.Line("var culture = global::System.Globalization.CultureInfo.CurrentCulture;")
            .Line("var uiCulture = global::System.Globalization.CultureInfo.CurrentUICulture;")
            .OpenBlock("try")
            .Line("global::System.Globalization.CultureInfo.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;")
            .Line("global::System.Globalization.CultureInfo.CurrentUICulture = global::System.Globalization.CultureInfo.InvariantCulture;")
            .OpenBlock("checked").RawVerbatim(body, requirementId).EndBlock().EndBlock()
            .OpenBlock("catch (global::System.Exception)")
            .Line(iterator ? "return [\"Validation body failed.\"];" : "return false;").EndBlock()
            .OpenBlock("finally")
            .Line("global::System.Globalization.CultureInfo.CurrentCulture = culture;")
            .Line("global::System.Globalization.CultureInfo.CurrentUICulture = uiCulture;").EndBlock();
    }
}
#endif
