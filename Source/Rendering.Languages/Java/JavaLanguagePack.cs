// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Validates the structured, sentinel-free java@1 input contract. Name and import resolution are separate work.
/// </summary>
public static class JavaLanguagePack
{
    /// <summary>
    /// Validates every input field, including metadata and external types, without modifying the plan.
    /// </summary>
    /// <param name="plan">The structured symbol plan.</param>
    /// <exception cref="LanguagePackRefusal">Input fields or literal streams violate integrity, a literal boundary is unsafe, or a gap/native context is unknown.</exception>
    public static void Validate(SymbolPlan plan)
    {
        foreach (var field in plan.ExternalTypes.SelectMany(type => type.Fields)) PrivateUseCodePoints.Validate(field);
        foreach (var file in plan.Files)
        {
            PrivateUseCodePoints.Validate(file.Id);
            PrivateUseCodePoints.Validate(file.Package);
            foreach (var segment in file.Segments)
            {
                foreach (var field in MetadataFields(segment)) PrivateUseCodePoints.Validate(field);
                switch (segment)
                {
                    case NativeSegment native:
                        ValidateContext(native.Context);
                        break;
                    case GapSegment gap:
                        ValidateContext(gap.Context);
                        break;
                }
            }

            ValidateLiteralStreams(file.Segments);
        }
    }

    static IEnumerable<string> MetadataFields(SymbolSegment segment) => segment switch
    {
        TextSegment => [],
        NativeSegment native => new[] { native.Language, native.Owner, native.Context }.Concat(native.Scope.SelectMany(type => type.Fields)),
        _ => segment.Fields,
    };

    static void ValidateLiteralStreams(IReadOnlyList<SymbolSegment> segments)
    {
        var stream = new StringBuilder();
        foreach (var segment in segments)
        {
            var literal = LiteralText(segment);
            if (literal is not null)
            {
                stream.Append(literal);
                continue;
            }

            var literalStream = stream.ToString();
            PrivateUseCodePoints.Validate(literalStream);
            if (JavaUnicodeEscapes.EndsWithEligibleBackslash(literalStream))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.UnsafeLiteralBoundary, "A Java literal stream must not end in an eligible backslash before an unresolved segment.");
            }

            stream.Clear();
        }

        PrivateUseCodePoints.Validate(stream.ToString());
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
