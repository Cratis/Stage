// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Conservatively pre-validates the structured, sentinel-free java@1 input contract as defense in depth.
/// Resolved output integrity is guaranteed only by <see cref="JavaOutputIntegrity"/>.
/// </summary>
public static class JavaLanguagePack
{
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
        }
    }

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
