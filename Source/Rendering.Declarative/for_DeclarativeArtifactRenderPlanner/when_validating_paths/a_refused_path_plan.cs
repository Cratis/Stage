// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_validating_paths;

/// <summary>
/// Base for path refusals: a kit whose outputs render the given paths.
/// </summary>
public abstract class a_refused_path_plan : given.a_declarative_planner
{
    private protected ArtifactRenderPlan _plan = null!;

    protected abstract string ExpectedCode { get; }

    internal static ArtifactRenderPlan PlanPaths(params string[] paths) => Plan(Profile(Archive(Files(paths))));

    static Dictionary<string, ImmutableArray<byte>> Files(string[] paths)
    {
        var files = new Dictionary<string, ImmutableArray<byte>>(StringComparer.Ordinal) { [Kits.KitLoader.ManifestPath] = Bytes(ManifestFor(paths)) };
        for (var index = 0; index < paths.Length; index++)
        {
            files[$"templates/t{index}.sbn"] = Bytes("index");
        }

        return files;
    }

    static string ManifestFor(string[] paths) =>
        "{\"apiVersion\":\"stage-target/v1.0\",\"target\":{\"id\":\"acme\",\"version\":\"1.0.0\"},\"languagePacks\":[\"test@1.0.0\"],\"requires\":[\"templates/v1.0\",\"model/v1.0\"],\"outputs\":["
        + string.Join(',', paths.Select((path, index) => $"{{\"id\":\"out{index}\",\"template\":\"templates/t{index}.sbn\",\"path\":\"{path}\"}}"))
        + "],\"partials\":[],\"tables\":[],\"assets\":[],\"diagnostics\":[]}";
}
