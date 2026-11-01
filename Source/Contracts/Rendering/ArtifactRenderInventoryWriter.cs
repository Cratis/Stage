// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderInventoryWriter
{
    internal static JsonWriterOptions Options => new() { Indented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    internal static byte[] Bytes(ArtifactRenderInventory inventory)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, Options);
        Write(writer, inventory);
        writer.Flush();
        stream.WriteByte(10);
        return stream.ToArray();
    }

    internal static void Write(Utf8JsonWriter writer, ArtifactRenderInventory inventory)
    {
        writer.WriteStartObject();
        writer.WriteString("version", ArtifactRenderInventory.Version);
        writer.WritePropertyName("score");
        WriteScore(writer, inventory.Score);
        WriteKinds(writer, inventory);
        writer.WriteStartArray("constructs");
        foreach (var construct in inventory.Constructs) WriteConstruct(writer, construct);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteKinds(Utf8JsonWriter writer, ArtifactRenderInventory inventory)
    {
        writer.WriteStartObject("kinds");
        foreach (var kind in Enum.GetValues<ArtifactRenderConstructKind>().OrderBy(kind => kind.ToString(), StringComparer.Ordinal))
        {
            writer.WriteStartObject(kind.ToString());
            writer.WriteNumber("weight", ArtifactRenderConstructWeights.For(kind));
            writer.WritePropertyName("score");
            WriteScore(writer, ArtifactRenderScore.For(inventory.Constructs.Where(construct => construct.Kind == kind)));
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    static void WriteScore(Utf8JsonWriter writer, ArtifactRenderScore score)
    {
        writer.WriteStartObject();
        writer.WriteNumber("rendered", score.Counts.Rendered);
        writer.WriteNumber("degraded", score.Counts.Degraded);
        writer.WriteNumber("refused", score.Counts.Refused);
        writer.WriteNumber("total", score.Counts.Total);
        writer.WriteNumber("earnedWeight", score.EarnedWeight);
        writer.WriteNumber("totalWeight", score.TotalWeight);
        if (score.BasisPoints is { } basisPoints) writer.WriteNumber("basisPoints", basisPoints);
        else writer.WriteNull("basisPoints");
        writer.WriteEndObject();
    }

    static void WriteConstruct(Utf8JsonWriter writer, ArtifactRenderConstruct construct)
    {
        writer.WriteStartObject();
        writer.WriteString("identity", construct.Identity.ToString());
        writer.WriteString("kind", construct.Kind.ToString());
        WriteLocator(writer, construct.SemanticLocator);
        writer.WriteString("modelPath", construct.ModelPath);
        writer.WriteString("outcome", construct.Outcome.ToCanonicalName());
        writer.WriteStartArray("degradations");
        foreach (var gap in construct.Degradations) WriteGap(writer, gap);
        writer.WriteEndArray();
        writer.WriteStartArray("refusalReasons");
        foreach (var reason in construct.RefusalReasons)
        {
            writer.WriteStartObject();
            writer.WriteString("code", reason.Code);
            writer.WriteString("message", reason.Message);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteLocator(Utf8JsonWriter writer, SemanticAddress locator)
    {
        writer.WriteStartObject("semanticLocator");
        writer.WriteString("kind", locator.Kind.ToString());
        writer.WriteStartArray("parts");
        foreach (var part in locator.Parts)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", part.Kind.ToString());
            writer.WriteString("key", part.Key);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteGap(Utf8JsonWriter writer, ArtifactRenderConstructGap gap)
    {
        writer.WriteStartObject();
        writer.WriteString("code", gap.Evidence.Diagnostic.Code);
        writer.WriteString("placeholderKind", gap.PlaceholderKind.ToString());
        writer.WriteString("intent", gap.Intent);
        WriteLocator(writer, gap.SemanticLocator);
        writer.WriteString("modelPath", gap.ModelPath);
        writer.WriteString("message", gap.Evidence.Diagnostic.Message);
        writer.WriteString("remedy", gap.Evidence.Remedy);
        writer.WriteStartArray("roots");
        foreach (var root in gap.Roots)
        {
            writer.WriteStartObject();
            writer.WriteString("owner", root.Owner.ToString());
            writer.WriteString("code", root.Code);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
