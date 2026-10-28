// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderReportWriter
{
    internal static byte[] Bytes(ArtifactRenderPlan plan, ArtifactRenderReportContext context)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, ArtifactRenderInventoryWriter.Options);
        writer.WriteStartObject();
        var ranged = context.Support.DeclaredRange is not null;
        writer.WriteString("schemaVersion", ranged ? ArtifactRenderReports.RangeSchemaVersion : ArtifactRenderReports.SchemaVersion);
        writer.WriteString("scoreVersion", ArtifactRenderConstructWeights.Version);
        writer.WriteString("inputLanguageVersion", context.Input.Language.ToString());
        writer.WriteString("inputSemanticVersion", context.Input.Semantics.ToString());
        WriteProvider(writer, plan);
        WriteSupport(writer, context);
        WritePacks(writer, context);
        WriteScopes(writer, plan);
        writer.WriteString("outcome", plan.Outcome.ToCanonicalName());
        writer.WriteBoolean("strict", plan.Strict);
        writer.WriteBoolean("success", plan.Success);
        writer.WriteBoolean("canPublishArtifacts", plan.CanPublishArtifacts);
        if (ranged) WritePlanEvidence(writer, plan);
        writer.WritePropertyName("inventory");
        ArtifactRenderInventoryWriter.Write(writer, plan.Inventory!);
        writer.WriteEndObject();
        writer.Flush();
        stream.WriteByte(10);
        return stream.ToArray();
    }

    static void WriteProvider(Utf8JsonWriter writer, ArtifactRenderPlan plan)
    {
        writer.WriteStartObject("provider");
        writer.WriteString("target", plan.Target);
        writer.WriteString("targetVersion", plan.TargetVersion);
        writer.WriteString("renderer", plan.Renderer);
        writer.WriteString("rendererVersion", plan.RendererVersion);
        writer.WriteEndObject();
    }

    static void WriteSupport(Utf8JsonWriter writer, ArtifactRenderReportContext context)
    {
        var support = context.Support;
        var range = support.DeclaredRange;
        writer.WriteStartObject("support");
        writer.WriteString("version", range is null ? ArtifactRenderAuditedSupport.Version : ArtifactRenderAuditedSupport.RangeVersion);
        writer.WriteString("policy", range is null ? ArtifactRenderAuditedSupport.Policy : ArtifactRenderAuditedSupport.RangePolicy);
        if (range is not null) WriteRange(writer, context, range);
        writer.WriteStartArray("auditedPairs");
        foreach (var pair in support.AuditedPairs)
        {
            writer.WriteStartObject();
            writer.WriteString("languageVersion", pair.Language.ToString());
            writer.WriteString("semanticVersion", pair.Semantics.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteRange(Utf8JsonWriter writer, ArtifactRenderReportContext context, ArtifactRenderDeclaredRange range)
    {
        writer.WriteStartObject("declaredRange");
        writer.WriteString("minimumLanguageVersion", range.Minimum.Language.ToString());
        writer.WriteString("minimumSemanticVersion", range.Minimum.Semantics.ToString());
        writer.WriteNumber("exclusiveMajor", range.ExclusiveMajor);
        writer.WriteEndObject();
        writer.WriteStartObject("effective");
        writer.WriteString("policy", context.Support.AuditedPairs.Contains(context.Input) ? "Audited" : "NewerWithinDeclaredRange");
        writer.WriteString("languageVersion", context.Input.Language.ToString());
        writer.WriteString("semanticVersion", context.Input.Semantics.ToString());
        writer.WriteEndObject();
    }

    static void WritePlanEvidence(Utf8JsonWriter writer, ArtifactRenderPlan plan)
    {
        writer.WriteStartArray("planEvidence");
        foreach (var gap in plan.PlanEvidence)
        {
            writer.WriteStartObject();
            writer.WriteString("code", gap.Diagnostic.Code);
            writer.WriteString("identity", gap.Diagnostic.Artifact.ToString());
            writer.WriteString("message", gap.Diagnostic.Message);
            writer.WriteString("remedy", gap.Remedy);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    static void WritePacks(Utf8JsonWriter writer, ArtifactRenderReportContext context)
    {
        writer.WriteStartArray("packs");
        foreach (var pack in context.Packs)
        {
            writer.WriteStartObject();
            writer.WriteString("name", pack.Name);
            writer.WriteString("version", pack.Version);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    static void WriteScopes(Utf8JsonWriter writer, ArtifactRenderPlan plan)
    {
        writer.WriteStartArray("scopes");
        foreach (var scope in ArtifactRenderReports.Scopes(plan))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", scope.Kind.ToString());
            writer.WriteString("identity", scope.Artifact.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
