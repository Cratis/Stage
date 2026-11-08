// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Carries the named sinks of an application in the hashed render profile. Sinks are realization metadata outside the portable
/// model, so choosing or dropping one leaves the model's revision unchanged; this input is what makes the change reach the digest.
/// </summary>
internal static class SinksInput
{
    internal const string Name = "cratis-realization:sinks";
    const string Version = "1";
    const string ReadModelsKind = "readmodels";
    const string GraphsKind = "graphs";

    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Adds the sinks to the profile, replacing any existing sinks input.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="sinks">The sinks of the application.</param>
    /// <returns>The profile with the sinks, or unchanged when the application has none.</returns>
    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, RealizedSinks sinks)
    {
        // Absence is the legacy profile: do not add an empty input or perturb its digest.
        if (sinks.IsEmpty)
        {
            return profile;
        }

        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(candidate => candidate.Name != Name), Create(sinks)]);
    }

    /// <summary>
    /// Reads the sinks of a profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="model">The model the profile renders, which every read model and graph the input names must belong to; <see langword="null"/> skips that check.</param>
    /// <param name="sinks">The sinks, <see cref="RealizedSinks.None"/> when the profile carries none.</param>
    /// <returns>Whether the profile carries no sinks input or exactly one canonical, consistent one.</returns>
    internal static bool TryRead(ArtifactRenderProfile profile, ExecutableSemanticModel? model, out RealizedSinks sinks)
    {
        sinks = RealizedSinks.None;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1 || inputs[0].Version != Version) return false;
        try
        {
            var wire = JsonSerializer.Deserialize<Wire>(inputs[0].Bytes.AsSpan(), _options);
            if (wire?.Sinks is null || wire.ReadModels is null || wire.Graphs is null) return false;
            var declarations = new List<RealizedSink>();
            foreach (var sink in wire.Sinks)
            {
                if (string.IsNullOrWhiteSpace(sink.Name) || Kind(sink.Kind) is not { } kind) return false;
                declarations.Add(new(sink.Name, kind));
            }

            if (!Consistent(declarations, wire)) return false;
            var readModels = ReadModels(wire, model);
            if (readModels is null || (model is not null && !GraphsDeclared(wire, model))) return false;
            var read = new RealizedSinks([.. declarations], readModels, wire.Graphs.ToImmutableDictionary(StringComparer.Ordinal));

            // Only a canonical encoding is admitted, so the digest of the profile identifies the sinks.
            var canonical = Create(read);
            if (canonical.Sha256 != inputs[0].Sha256 || !canonical.Bytes.SequenceEqual(inputs[0].Bytes)) return false;
            sinks = read;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool Consistent(List<RealizedSink> declarations, Wire wire)
    {
        var byName = new Dictionary<string, SemanticSinkKind>(StringComparer.OrdinalIgnoreCase);
        foreach (var declaration in declarations)
        {
            if (!byName.TryAdd(declaration.Name, declaration.Kind)) return false;
        }

        bool Names(string sink, SemanticSinkKind kind) => declarations.Exists(declaration => declaration.Kind == kind && string.Equals(declaration.Name, sink, StringComparison.Ordinal));
        return wire.ReadModels!.Values.All(sink => Names(sink, SemanticSinkKind.ReadModels)) && wire.Graphs!.Values.All(sink => Names(sink, SemanticSinkKind.Graphs));
    }

    static ImmutableDictionary<SemanticId, string>? ReadModels(Wire wire, ExecutableSemanticModel? model)
    {
        if (model is null)
        {
            // A profile read without its model keeps the identities it names, parsed back the way the model would hold them.
            var parsed = ImmutableDictionary.CreateBuilder<SemanticId, string>();
            foreach (var (id, sink) in wire.ReadModels!)
            {
                if (!SemanticId.TryParse(id, out var semanticId)) return null;
                parsed[semanticId] = sink;
            }

            return parsed.ToImmutable();
        }

        var declared = SemanticVersionFeatures.Slices(model.Application).SelectMany(slice => slice.ReadModels)
            .ToDictionary(readModel => readModel.Id.ToString(), readModel => readModel.Id, StringComparer.Ordinal);
        if (wire.ReadModels!.Keys.Any(id => !declared.ContainsKey(id))) return null;
        return wire.ReadModels!.ToImmutableDictionary(entry => declared[entry.Key], entry => entry.Value);
    }

    static bool GraphsDeclared(Wire wire, ExecutableSemanticModel model)
    {
        var graphs = model.Application.Graphs.IsDefault ? [] : model.Application.Graphs;
        return wire.Graphs!.Keys.All(name => graphs.Any(graph => string.Equals(graph.Name, name, StringComparison.Ordinal)));
    }

    static SemanticSinkKind? Kind(string? kind) => kind switch
    {
        ReadModelsKind => SemanticSinkKind.ReadModels,
        GraphsKind => SemanticSinkKind.Graphs,
        _ => null
    };

    static ArtifactRenderInput Create(RealizedSinks sinks) => ArtifactRenderInput.Create(Name, Version, [.. Serialize(sinks)]);

    static byte[] Serialize(RealizedSinks sinks) =>
        JsonSerializer.SerializeToUtf8Bytes(
            new Wire(
                [.. sinks.Declarations.Select(sink => new WireSink(sink.Name, sink.Kind == SemanticSinkKind.Graphs ? GraphsKind : ReadModelsKind))],
                new SortedDictionary<string, string>(sinks.ReadModels.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value, StringComparer.Ordinal), StringComparer.Ordinal),
                new SortedDictionary<string, string>(sinks.Graphs, StringComparer.Ordinal)),
            _options);

    sealed record WireSink(string? Name, string? Kind);

    sealed record Wire(WireSink[]? Sinks, SortedDictionary<string, string>? ReadModels, SortedDictionary<string, string>? Graphs);
}
