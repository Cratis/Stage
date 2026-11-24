// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders personal-data classification and its audit protection without changing subject encryption.
/// </summary>
/// <param name="SpecialCategory">The declared special-category token, if any.</param>
/// <param name="Criminal">Whether the concept contains criminal-offense data.</param>
/// <param name="OperationalSecret">Whether the personal data is also an operational secret.</param>
internal sealed record PersonalConceptCompliance(string? SpecialCategory, bool Criminal, bool OperationalSecret)
{
    static readonly Dictionary<string, string> _specialCategories = new(StringComparer.Ordinal)
    {
        ["racialOrEthnicOrigin"] = "RacialOrEthnicOrigin",
        ["politicalOpinions"] = "PoliticalOpinions",
        ["religiousOrPhilosophicalBeliefs"] = "ReligiousOrPhilosophicalBeliefs",
        ["tradeUnionMembership"] = "TradeUnionMembership",
        ["genetic"] = "Genetic",
        ["biometric"] = "Biometric",
        ["health"] = "Health",
        ["sexLifeOrSexualOrientation"] = "SexLifeOrSexualOrientation"
    };

    internal void Render(CSharpCodeBuilder builder, bool qualified)
    {
        var arguments = Arguments(builder);
        var attribute = AttributeType(builder, "Cratis.Chronicle.Compliance.GDPR", "PII", qualified);
        builder.Attribute(arguments.Count == 0 ? attribute : $"{attribute}({string.Join(", ", arguments)})");
        if (OperationalSecret)
        {
            builder.Attribute(AttributeType(builder, "Cratis.Arc.Chronicle.Commands", "NotAudited", qualified));
        }
    }

    static string AttributeType(CSharpCodeBuilder builder, string @namespace, string name, bool qualified)
    {
        if (!qualified) builder.Using(@namespace);
        return qualified
            ? builder.Type(new(@namespace, $"{name}Attribute", 0, ReferenceContext.Attribute))
            : name;
    }

    List<string> Arguments(CSharpCodeBuilder builder)
    {
        var arguments = new List<string>();
        if (SpecialCategory is { } category)
        {
            var categoryType = builder.Type(new("Cratis.Chronicle.Compliance.GDPR", "SpecialCategory", 0, ReferenceContext.Expression));
            arguments.Add($"SpecialCategory = {categoryType}.{_specialCategories[category]}");
        }

        if (Criminal) arguments.Add("Criminal = true");
        if (OperationalSecret) arguments.Add("OperationalSecret = true");
        return arguments;
    }
}
