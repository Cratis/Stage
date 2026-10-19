// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Carries a host's transient reducer-migration evidence and removal capability. It is never persisted.
/// </summary>
internal static class ReducerMigrationInput
{
    internal const string Name = "cratis-rendering:reducer-migration";

    internal static ArtifactRenderInput Create(IEnumerable<ReducerMigrationEvidence> evidence, bool canRemoveFiles)
    {
        var entries = evidence.OrderBy(entry => entry.ReadModel.ToString(), StringComparer.Ordinal).Select(entry => new
        {
            readModel = entry.ReadModel.ToString(),
            relativePath = entry.RelativePath,
            sha256 = entry.Sha256,
            observerId = entry.ObserverId,
            readModelType = entry.ReadModelType,
            refusal = entry.Refusal
        });
        var json = JsonSerializer.Serialize(new { version = 1, removalCapability = canRemoveFiles, evidence = entries }) + "\n";
        return ArtifactRenderInput.Create(Name, "1", [.. Encoding.UTF8.GetBytes(json)]);
    }

    internal static bool TryRead(ArtifactRenderProfile profile, out Migration migration)
    {
        migration = Migration.None;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1 || inputs[0].Version != "1") return false;
        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()));
            var root = document.RootElement;
            var capability = root.GetProperty("removalCapability");
            if (root.GetProperty("version").GetInt32() != 1 || capability.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
            var evidence = root.GetProperty("evidence").EnumerateArray().Select(entry => new ReducerMigrationEvidence(
                SemanticId.Parse(entry.GetProperty("readModel").GetString()!),
                entry.GetProperty("relativePath").GetString()!,
                entry.GetProperty("sha256").GetString()!,
                entry.GetProperty("observerId").GetString()!,
                entry.GetProperty("readModelType").GetString()!,
                entry.GetProperty("refusal").GetString())).ToImmutableArray();
            var parsed = new Migration(capability.GetBoolean(), evidence);
            if (evidence.IsEmpty || evidence.Select(entry => entry.ReadModel).Distinct().Count() != evidence.Length ||
                !inputs[0].Bytes.SequenceEqual(Create(evidence, parsed.CanRemoveFiles).Bytes))
            {
                return false;
            }

            migration = parsed;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException or DecoderFallbackException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Represents the admitted transient migration request.
    /// </summary>
    /// <param name="CanRemoveFiles">Whether the host declared it can delete files atomically with its writes.</param>
    /// <param name="Evidence">The evidence in ordinal read-model order.</param>
    internal sealed record Migration(bool CanRemoveFiles, ImmutableArray<ReducerMigrationEvidence> Evidence)
    {
        internal static readonly Migration None = new(false, []);
    }
}
