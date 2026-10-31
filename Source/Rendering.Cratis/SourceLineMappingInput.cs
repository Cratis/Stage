// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 source line mapping input: spliced inline authored bodies are wrapped in <c language="csharp">#line</c>
/// directives so a compiler error in a body is reported at the <c language="text">.play</c> file and line. Its absence, the default,
/// emits no directives.
/// </summary>
public static class SourceLineMappingInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:source-lines";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Creates the source line mapping input.
    /// </summary>
    /// <param name="modelRoot">The path from the destination root to the folder holding the <c language="text">.play</c> files, with <c language="text">/</c> separators; empty when they share the destination root.</param>
    /// <param name="documents">The document identities with their display paths relative to the model root.</param>
    /// <returns>The hashed profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A path is rooted, escapes its folder, or contains a backslash, colon, quote or control character.</exception>
    public static ArtifactRenderInput Create(string modelRoot, IEnumerable<KeyValuePair<DocumentId, string>> documents)
    {
        ArgumentNullException.ThrowIfNull(modelRoot);
        ArgumentNullException.ThrowIfNull(documents);

        if (!IsValidRoot(modelRoot)) throw new InvalidArtifactRenderContract("The source line mapping model root is not a relative path made of plain segments.");

        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (id, path) in documents)
        {
            if (!id.IsSet || !IsValidDisplayPath(path)) throw new InvalidArtifactRenderContract("A source line mapping document has no identity or an invalid display path.");
            entries[id.ToString()] = path;
        }

        return ArtifactRenderInput.Create(Name, Version, [.. JsonSerializer.SerializeToUtf8Bytes(new Payload(entries, modelRoot), _options)]);
    }

    // Reads the source line mapping a profile carries: true with a null mapping when it carries none, false when it carries a duplicate or invalid input.
    internal static bool TryRead(ArtifactRenderProfile profile, out Mapping? mapping)
    {
        mapping = null;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        return inputs.Length == 0 || (inputs is [var only] && TryRead(only, out mapping));
    }

    static bool TryRead(ArtifactRenderInput input, out Mapping? mapping)
    {
        mapping = null;
        if (input.Version != Version) return false;

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(input.Bytes.AsSpan(), _options);
            if (!IsValidPayload(payload)) return false;
            if (!TryParseDocuments(payload, out var documents)) return false;
            if (!input.Bytes.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(new Payload(new(payload.Documents, StringComparer.Ordinal), payload.Root), _options))) return false;

            mapping = new(payload.Root, documents);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool IsValidPayload(Payload? payload) =>
        payload?.Documents is not null && payload.Root is not null && IsValidRoot(payload.Root);

    static bool TryParseDocuments(Payload payload, out Dictionary<DocumentId, string> documents)
    {
        documents = [];
        foreach (var (key, path) in payload.Documents)
        {
            if (!DocumentId.TryParse(key, out var id) || id.ToString() != key || !IsValidDisplayPath(path)) return false;
            documents[id] = path;
        }

        return true;
    }

    static bool IsValidRoot(string root)
    {
        if (root.Length == 0) return true;

        var segments = root.Split('/');
        var leading = segments.TakeWhile(segment => segment == "..").Count();
        return segments.Skip(leading).All(IsPlainSegment);
    }

    static bool IsValidDisplayPath(string? path) =>
        !string.IsNullOrEmpty(path) && path.Split('/').All(IsPlainSegment);

    static bool IsPlainSegment(string segment) =>
        segment.Length > 0 && segment != "." && segment != ".."
        && !segment.Any(character => character is '\\' or ':' or '"' || char.IsControl(character));

    internal sealed record Mapping(string Root, IReadOnlyDictionary<DocumentId, string> Documents)
    {
        internal string? PathOf(DocumentId document)
        {
            if (!Documents.TryGetValue(document, out var path)) return null;
            return Root.Length == 0 ? path : $"{Root}/{path}";
        }
    }

    sealed record Payload(SortedDictionary<string, string> Documents, string Root);
}
