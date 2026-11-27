// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Maps concept protection markers, with compliance details enabled by default.
/// </summary>
internal static class ConceptComplianceRendering
{
    internal static void Render(CSharpCodeBuilder builder, ConceptSyntax concept, bool complianceDetails)
    {
        var pii = concept.Attributes.FirstOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Pii);
        var secret = concept.Attributes.FirstOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Sensitive);
        if (pii is not null)
        {
            new PersonalConceptCompliance(pii.SpecialCategory, pii.Criminal, secret is not null).Render(builder, false);
            RenderDetails(builder, pii, complianceDetails);
        }
        else if (secret is not null)
        {
            Attribute(builder, "Cratis.Chronicle.ProtectedValues", Encrypted(secret.Scope, secret.Reason, complianceDetails));
            Attribute(builder, "Cratis.Arc.Chronicle.Commands", "NotAudited");
        }
    }

    internal static void Render(CSharpCodeBuilder builder, SemanticConceptCompliance? compliance)
    {
        if (compliance?.Personal is { } personal)
        {
            new PersonalConceptCompliance(personal.Special, personal.Criminal, compliance.Secret is not null).Render(builder, true);
        }
        else if (compliance?.Secret is { } secret)
        {
            var encrypted = builder.Type(new("Cratis.Chronicle.ProtectedValues", "EncryptedAttribute", 0, ReferenceContext.Attribute));
            var scope = Identifiers.ToPascalCase(secret.Scope ?? "subject");
            if (scope != "Subject") encrypted += $"({builder.Type(new("Cratis.Chronicle.ProtectedValues", "EncryptionScope", 0, ReferenceContext.Expression))}.{scope})";
            builder.Attribute(encrypted)
                .Attribute(builder.Type(new("Cratis.Arc.Chronicle.Commands", "NotAuditedAttribute", 0, ReferenceContext.Attribute)));
        }
    }

    static void RenderDetails(CSharpCodeBuilder builder, ConceptAttributeSyntax pii, bool complianceDetails)
    {
        if (!complianceDetails) return;
        var details = Details(pii);
        if (details.Length > 0)
        {
            Attribute(builder, "Cratis.Chronicle.Compliance", $"ComplianceDetails({CSharpCodeBuilder.StringLiteral(details)})");
        }
    }

    static void Attribute(CSharpCodeBuilder builder, string @namespace, string attribute)
    {
        builder.Using(@namespace).Attribute(attribute);
    }

    static string Encrypted(string? declaredScope, string? reason, bool complianceDetails)
    {
        var scope = Identifiers.ToPascalCase(declaredScope ?? "subject");
        if (complianceDetails && (declaredScope is not null || reason is not null))
        {
            return $"Encrypted(EncryptionScope.{scope}, {CSharpCodeBuilder.StringLiteral(reason ?? string.Empty)})";
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
