// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Resolves the structured, sentinel-free java@1 contract with conservative binding and final output integrity checks.
/// </summary>
public static class JavaLanguagePack
{
    /// <summary>
    /// Validates and resolves a complete plan without normalizing authored native bytes.
    /// </summary>
    /// <param name="plan">The structured, sentinel-free input supplied by the planner.</param>
    /// <returns>Resolved files, allocated primary types, and caller-coded semantic-gap diagnostics in deterministic order.</returns>
    /// <exception cref="LanguagePackRefusal">The input contract, a binding, or final output integrity cannot be proved.</exception>
    public static LanguagePackResolution Resolve(SymbolPlan plan)
    {
        Validate(plan);
        var resolver = new JavaFileResolver(new JavaDeclarations(plan));
        var files = plan.Files.OrderBy(file => file.Id, StringComparer.Ordinal).Select(resolver.Resolve).ToArray();
        var diagnostics = plan.Files.SelectMany(file => file.Segments.Select((segment, index) => (segment, index))
            .Where(item => item.segment is GapSegment).Select(item => Diagnostic(file.Id, item.index, (GapSegment)item.segment)))
            .OrderBy(diagnostic => diagnostic.File, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Segment)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal).ToArray();

        return new(files, diagnostics);
    }

    /// <summary>
    /// Validates every input field, including metadata and external types, without modifying the plan.
    /// </summary>
    /// <param name="plan">The structured symbol plan.</param>
    /// <exception cref="LanguagePackRefusal">An input field violates integrity, a literal boundary is unsafe, or a gap/native context is unknown.</exception>
    public static void Validate(SymbolPlan plan)
    {
        foreach (var field in plan.ExternalTypes.SelectMany(type => type.Fields).Concat(plan.ExternalDeclarations.SelectMany(type => type.Fields))) PrivateUseCodePoints.Validate(field);
        foreach (var file in plan.Files)
        {
            PrivateUseCodePoints.Validate(file.Id);
            PrivateUseCodePoints.Validate(file.Package);
            for (var index = 0; index < file.Segments.Count; index++)
            {
                var segment = file.Segments[index];
                foreach (var field in segment.Fields) PrivateUseCodePoints.Validate(field);
                switch (segment)
                {
                    case NativeSegment native:
                        ValidateContext(native.Context);
                        break;
                    case GapSegment gap:
                        ValidateContext(gap.Context);
                        break;
                }

                if (index + 1 < file.Segments.Count) ValidateLiteralBoundary(segment);
            }

            InlineBodyScanner.ValidateDeclarations(file);
        }
    }

    static LanguagePackDiagnostic Diagnostic(string file, int index, GapSegment gap) => new(file, index, gap.Code, gap.Construct, gap.Intent, gap.Context);

    static void ValidateLiteralBoundary(SymbolSegment segment)
    {
        var literal = LiteralText(segment);
        if (literal is null) return;
        if (literal.EndsWith('\\') || JavaUnicodeEscapes.Decode(literal).EndsWith('\\'))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.UnsafeLiteralBoundary, "A Java literal span must not end in a raw or decoded backslash when another segment follows.");
        }
    }

    static string? LiteralText(SymbolSegment segment) => segment switch
    {
        TextSegment text => text.Text,
        NativeSegment native => native.Body,
        _ => null,
    };

    static void ValidateContext(string context)
    {
        if (context is not ("type" or "member" or "statement"))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.UnsupportedContext, "The java@1 gap and native contexts are type, member, and statement.");
        }
    }
}
