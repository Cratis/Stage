// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

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
        RenderProtection(builder, new(pii is not null, pii is not null && complianceDetails ? Details(pii) : string.Empty, secret is not null ? Encrypted(secret.Scope, secret.Reason, complianceDetails, false) : null), false);
    }

    internal static void Render(CSharpCodeBuilder builder, SemanticConceptCompliance? compliance)
    {
        if (compliance?.Personal is not null)
        {
            builder.Attribute(builder.Type(new("Cratis.Chronicle.Compliance.GDPR", "PIIAttribute", 0, ReferenceContext.Attribute)));
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

    static void RenderProtection(CSharpCodeBuilder builder, Protection protection, bool qualified)
    {
        if (protection.Personal)
        {
            Attribute(builder, "Cratis.Chronicle.Compliance.GDPR", qualified ? "PIIAttribute" : "PII", qualified);
            if (protection.Details.Length > 0)
            {
                Attribute(builder, "Cratis.Chronicle.Compliance", $"ComplianceDetails({CSharpCodeBuilder.StringLiteral(protection.Details)})", qualified);
            }
        }
        else if (protection.Encrypted is { } encrypted)
        {
            Attribute(builder, "Cratis.Chronicle.ProtectedValues", encrypted, qualified);
            Attribute(builder, "Cratis.Arc.Chronicle.Commands", qualified ? "NotAuditedAttribute" : "NotAudited", qualified);
        }
    }

    static void Attribute(CSharpCodeBuilder builder, string @namespace, string attribute, bool qualified)
    {
        if (!qualified) builder.Using(@namespace);
        builder.Attribute(qualified ? $"global::{@namespace}.{attribute}" : attribute);
    }

    static string Encrypted(string? declaredScope, string? reason, bool complianceDetails, bool qualified)
    {
        var scope = Identifiers.ToPascalCase(declaredScope ?? "subject");
        var attribute = qualified ? "EncryptedAttribute" : "Encrypted";
        var scopeType = qualified ? "global::Cratis.Chronicle.ProtectedValues.EncryptionScope" : "EncryptionScope";
        if (complianceDetails && (declaredScope is not null || reason is not null))
        {
            return $"{attribute}({scopeType}.{scope}, {CSharpCodeBuilder.StringLiteral(reason ?? string.Empty)})";
        }

        return scope == "Subject" ? attribute : $"{attribute}({scopeType}.{scope})";
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

    sealed record Protection(bool Personal, string Details, string? Encrypted);
}
