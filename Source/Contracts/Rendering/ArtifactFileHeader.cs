// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Owns the optional hashed file header applied to final artifacts by Stage.
/// </summary>
public static class ArtifactFileHeader
{
    /// <summary>
    /// The canonical profile input name.
    /// </summary>
    public const string InputName = "stage:file-header";

    /// <summary>
    /// The canonical JSON string-array format version.
    /// </summary>
    public const string InputVersion = "1";

    /// <summary>
    /// Identifies the canonical input and the two legacy target aliases.
    /// </summary>
    /// <param name="input">The profile input.</param>
    /// <returns>Whether Stage owns the input.</returns>
    public static bool IsInput(ArtifactRenderInput input) => input.Name == InputName || input.Name == "cratis-render:file-header" || input.Name == "axon-render:file-header";

    /// <summary>
    /// Validates single-line Unicode text without controls, private-use or bidi formatting characters.
    /// </summary>
    /// <param name="lines">The plain-text header lines; empty means no header.</param>
    /// <returns>Whether every line can safely appear in a line comment.</returns>
    public static bool IsValid(ImmutableArray<string> lines) => lines.IsDefaultOrEmpty || lines.All(IsValidLine);

    /// <summary>
    /// Creates the canonical hashed header input.
    /// </summary>
    /// <param name="lines">One or more safe plain-text lines.</param>
    /// <returns>The canonical JSON string-array input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The lines are empty or unsafe.</exception>
    public static ArtifactRenderInput CreateInput(ImmutableArray<string> lines)
    {
        if (lines.IsDefaultOrEmpty || !IsValid(lines))
        {
            throw new InvalidArtifactRenderContract("A file header input requires one or more safe, single-line, non-null lines.");
        }
        return ArtifactRenderInput.Create(InputName, InputVersion, [.. JsonSerializer.SerializeToUtf8Bytes(lines.ToArray())]);
    }

    /// <summary>
    /// Adds the header input, or returns the unchanged profile for empty lines.
    /// </summary>
    /// <param name="profile">The resolved render profile.</param>
    /// <param name="lines">The plain-text lines.</param>
    /// <returns>The profile with one canonical header input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A header is already present or the lines are unsafe.</exception>
    public static ArtifactRenderProfile With(ArtifactRenderProfile profile, ImmutableArray<string> lines)
    {
        if (lines.IsDefaultOrEmpty) return profile;
        if (profile.Inputs.Any(IsInput)) throw new InvalidArtifactRenderContract("The profile already carries a file header input.");
        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, CreateInput(lines)]);
    }

    /// <summary>
    /// Reads at most one canonical header or legacy alias.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="lines">The lines, or empty when absent or invalid.</param>
    /// <returns>Whether the input is absent or canonical and valid.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out ImmutableArray<string> lines)
    {
        lines = [];
        var inputs = profile.Inputs.Where(IsInput).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1) return false;
        return TryReadInput(inputs[0], out lines);
    }

    /// <summary>
    /// Formats trimmed lines and a final blank line using a language's comment syntax.
    /// </summary>
    /// <param name="lines">The validated header lines.</param>
    /// <param name="syntax">The language's line comment syntax.</param>
    /// <returns>The LF-normalized comment block.</returns>
    public static string Comment(ImmutableArray<string> lines, ArtifactCommentSyntax syntax) =>
        string.Concat(lines.Select(line => line.TrimEnd() is { Length: > 0 } text ? $"{syntax.LinePrefix} {text}\n" : $"{syntax.LinePrefix}\n")) + "\n";

    /// <summary>
    /// Applies the header to final artifacts in pipelines that publish without an artifact render plan.
    /// Planners and targets must not call this: <see cref="ArtifactRenderPlan.Create(ArtifactRenderRequest, ImmutableArray{PlannedArtifact}, ImmutableArray{ArtifactRenderDiagnostic})"/> owns their header.
    /// </summary>
    /// <param name="profile">The resolved profile.</param>
    /// <param name="artifacts">The final artifacts.</param>
    /// <param name="syntaxes">The language comment syntaxes.</param>
    /// <param name="owner">The semantic diagnostic owner.</param>
    /// <returns>The headed artifacts, or refusal diagnostics and no artifacts.</returns>
    public static ArtifactFileHeaderResult Apply(ArtifactRenderProfile profile, ImmutableArray<PlannedArtifact> artifacts, ArtifactCommentSyntaxes syntaxes, SemanticId owner = default)
    {
        if (!TryRead(profile, out var lines)) return new([], [new("STAGE-HEADER-001", ArtifactRenderDiagnosticSeverity.Error, "The file header input is malformed, duplicated, or contains unsafe text.", owner)]);
        if (lines.IsEmpty) return new(artifacts, []);
        var diagnostics = UnsafeSyntaxDiagnostics(artifacts, syntaxes, lines, owner);
        if (!diagnostics.IsEmpty) return new([], diagnostics);
        var input = profile.Inputs.Single(IsInput);
        var shifts = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        var result = artifacts.Select(artifact =>
        {
            if (!NeedsHeader(artifact, input)) return artifact;
            if (!syntaxes.TryGet(artifact.RelativePath, out var syntax)) return artifact;
            var block = Comment(lines, syntax);
            shifts.Add(artifact.RelativePath, block.Length);
            return PlannedArtifact.CreateText(artifact.RelativePath, block + Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), artifact.Sources).WithFileHeader(input);
        }).ToImmutableArray();
        return new(result, []) { Prepended = shifts.ToImmutable() };
    }

    static ImmutableArray<ArtifactRenderDiagnostic> UnsafeSyntaxDiagnostics(ImmutableArray<PlannedArtifact> artifacts, ArtifactCommentSyntaxes syntaxes, ImmutableArray<string> lines, SemanticId owner) =>
        [.. artifacts.Where(artifact => artifact.Kind == PlannedArtifactKind.Text && syntaxes.TryGet(artifact.RelativePath, out var syntax) &&
                syntax.RefusedSequences.Any(sequence => lines.Any(line => line.Contains(sequence, StringComparison.Ordinal))))
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .GroupBy(artifact => artifact.RelativePath[artifact.RelativePath.LastIndexOf('.')..].ToLowerInvariant(), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ArtifactRenderDiagnostic("STAGE-HEADER-002", ArtifactRenderDiagnosticSeverity.Error, $"The file header cannot be represented safely in '{group.First().RelativePath}' ({group.Key}).", owner))];

    static bool TryReadInput(ArtifactRenderInput input, out ImmutableArray<string> lines)
    {
        lines = [];
        if (input.Version != InputVersion) return false;
        try
        {
            var read = JsonSerializer.Deserialize<string[]>(new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan()));
            if (read is null) return false;
            if (read.Length == 0) return false;
            if (!IsValid([.. read])) return false;
            if (!CreateInput([.. read]).Bytes.SequenceEqual(input.Bytes)) return false;
            lines = [.. read];
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            return false;
        }
    }

    static bool NeedsHeader(PlannedArtifact artifact, ArtifactRenderInput input) => artifact.Kind == PlannedArtifactKind.Text && artifact.AppliedFileHeaderSha256 != input.Sha256;

    static bool IsRefused(Rune rune)
    {
        if (rune.Value == '\t') return false;
        if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.PrivateUse or UnicodeCategory.Surrogate) return true;
        return rune.Value is 0x061C or 0x200E or 0x200F or (>= 0x202A and <= 0x202E) or (>= 0x2066 and <= 0x2069);
    }

    static bool IsValidLine(string line)
    {
        if (line is null) return false;
        var remaining = line.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done)
            {
                return false;
            }
            if (IsRefused(rune))
            {
                return false;
            }
            remaining = remaining[consumed..];
        }
        return true;
    }
}
