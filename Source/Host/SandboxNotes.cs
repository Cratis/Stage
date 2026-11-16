// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.Published;
using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host;

/// <summary>
/// The configuration that lets a sandbox host (a play session) take feedback through the Notes backend. Off unless the host
/// sets <c language="shell">Stage:Sandbox:Notes:Enabled</c>; everything else about Notes is the <c language="shell">Notes</c> section the published host reads.
/// </summary>
/// <param name="Enabled">Whether the sandbox hosts the Notes backend.</param>
/// <param name="ModelReferencesFile">An optional JSON file of <c language="shell">kind:Name</c> to identifier, as for the exporter's references.</param>
internal sealed record SandboxNotes(bool Enabled, string? ModelReferencesFile)
{
    internal const string Section = "Stage:Sandbox:Notes";

    internal static SandboxNotes Read(IConfiguration configuration) => new(
        bool.TryParse(configuration[$"{Section}:Enabled"], out var enabled) && enabled,
        configuration[$"{Section}:ModelReferencesFile"] is { Length: > 0 } file ? file : null);

    /// <summary>
    /// Builds what the feedback button reads from <c language="shell">/stage/model-references</c>. A sandbox has no package manifest, so
    /// the identity comes from the Notes options and the references from the optional file.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="model">The loaded model, to check the references against.</param>
    /// <param name="scene">The scene of the model, or <see langword="null"/> when none could be built.</param>
    /// <returns>The document.</returns>
    internal object Document(IConfiguration configuration, ExecutableSemanticModel model, SceneApplication? scene)
    {
        return Describe(configuration, ReadReferences(model, scene));
    }

    internal static object Describe(IConfiguration configuration, IReadOnlyList<ModelReference> references)
    {
        var options = NotesPublishedExtension.Bind(configuration);
        return new
        {
            applicationName = options.ApplicationName.Value,
            publishedName = options.ApplicationName.Value,
            applicationVersion = options.ApplicationVersion,
            publicationId = options.PublicationId.Value,
            references,
            notesEnabled = true
        };
    }

    IReadOnlyList<ModelReference> ReadReferences(ExecutableSemanticModel model, SceneApplication? scene)
    {
        if (ModelReferencesFile is null || scene is null || !File.Exists(ModelReferencesFile))
        {
            return [];
        }

        var supplied = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ModelReferencesFile)) ?? [];
        return ModelReferenceMap.Build(ModelReferenceMap.Keys(model, scene), supplied);
    }
}
