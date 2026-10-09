// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Maps concept protection markers, keeping registration-changing details opt-in.
/// </summary>
internal static class ConceptComplianceRendering
{
    internal static void Render(CSharpCodeBuilder builder, ConceptSyntax concept, bool complianceDetails)
    {
        var pii = concept.Attributes.FirstOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Pii);
        var secret = concept.Attributes.FirstOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Sensitive);
        if (pii is not null)
        {
            builder.Using("Cratis.Chronicle.Compliance.GDPR").Attribute("PII");
            if (complianceDetails && Details(pii) is { Length: > 0 } details)
            {
                builder.Using("Cratis.Chronicle.Compliance").Attribute($"ComplianceDetails({CSharpCodeBuilder.StringLiteral(details)})");
            }
        }
        else if (secret is not null)
        {
            builder.Using("Cratis.Chronicle.ProtectedValues").Attribute(Encrypted(secret, complianceDetails))
                .Using("Cratis.Arc.Chronicle.Commands").Attribute("NotAudited");
        }
    }

    static string Encrypted(ConceptAttributeSyntax secret, bool complianceDetails)
    {
        var scope = Identifiers.ToPascalCase(secret.Scope ?? "subject");
        if (complianceDetails && (secret.Scope is not null || secret.Reason is not null))
        {
            return $"Encrypted(EncryptionScope.{scope}, {CSharpCodeBuilder.StringLiteral(secret.Reason ?? string.Empty)})";
        }

        return scope == "Subject" ? "Encrypted" : $"Encrypted(EncryptionScope.{scope})";
    }

    static string Details(ConceptAttributeSyntax pii)
    {
        var parts = new List<string>();
        if (pii.SpecialCategory is { } category)
        {
            parts.Add($"GDPR Art. 9(1) special category: {Identifiers.ToWords(category)}.");
        }

        if (pii.Criminal)
        {
            parts.Add("GDPR Art. 10 criminal offence data.");
        }

        if (pii.Reason is { Length: > 0 } reason)
        {
            parts.Add(reason);
        }

        return string.Join(' ', parts);
    }
}
