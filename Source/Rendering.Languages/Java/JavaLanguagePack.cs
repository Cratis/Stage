// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <exception cref="LanguagePackRefusal">An input field violates integrity or a gap/native context is unknown.</exception>
    public static void Validate(SymbolPlan plan)
    {
        foreach (var field in plan.ExternalTypes.SelectMany(type => type.Fields)) PrivateUseCodePoints.Validate(field);
        foreach (var file in plan.Files)
        {
            PrivateUseCodePoints.Validate(file.Id);
            PrivateUseCodePoints.Validate(file.Package);
            foreach (var segment in file.Segments)
            {
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
            }
        }
    }

    static void ValidateContext(string context)
    {
        if (context is not ("type" or "member" or "statement"))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.UnsupportedContext, "The java@1 gap and native contexts are type, member, and statement.");
        }
    }
}
