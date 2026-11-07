// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Adds the event contracts project to the application scaffold: the project itself, the application's reference to it and the solution entry.
/// </summary>
internal static class EventContractsScaffold
{
    const string ProjectMarker = "  <Import Project=\"Customizations/Dependencies.props\"";
    const string SolutionMarker = "</Solution>";

    /// <summary>
    /// Patches the root project file and the solution file; any other artifact passes through unchanged.
    /// </summary>
    /// <param name="artifact">The scaffold artifact.</param>
    /// <param name="contractsFolder">The contracts project folder and name.</param>
    /// <returns>The patched artifact, or the artifact itself.</returns>
    internal static PlannedArtifact Patch(PlannedArtifact artifact, string contractsFolder)
    {
        var path = artifact.RelativePath;
        if (path.Contains('/', StringComparison.Ordinal)) return artifact;
        if (path.EndsWith(".csproj", StringComparison.Ordinal)) return Rewrite(artifact, text => InsertBefore(text, ProjectMarker, Reference(contractsFolder)));
        return path.EndsWith(".slnx", StringComparison.Ordinal)
            ? Rewrite(artifact, text => InsertBefore(text, SolutionMarker, $"  <Project Path=\"{contractsFolder}/{contractsFolder}.csproj\" />\n"))
            : artifact;
    }

    /// <summary>
    /// Renders the contracts project file. The assembly names the Chronicle event store the events belong to, which makes every
    /// event in it public without a per-type attribute; a deployment that configures another event store treats them as foreign.
    /// </summary>
    /// <param name="contractsFolder">The contracts project folder and name.</param>
    /// <param name="eventStore">The Chronicle event store name, which is the application name.</param>
    /// <param name="sources">The semantic sources the project derives from.</param>
    /// <returns>The project artifact.</returns>
    internal static PlannedArtifact Project(string contractsFolder, string eventStore, ImmutableArray<SemanticId> sources) =>
        PlannedArtifact.CreateText(
            $"{contractsFolder}/{contractsFolder}.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Cratis.Chronicle" />
              </ItemGroup>
              <ItemGroup>
                <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
                  <_Parameter1>{SecurityElement.Escape(eventStore)}</_Parameter1>
                </AssemblyAttribute>
              </ItemGroup>
            </Project>

            """,
            sources);

    static string Reference(string contractsFolder) =>
        $"  <PropertyGroup>\n    <DefaultItemExcludes>$(DefaultItemExcludes);{contractsFolder}/**</DefaultItemExcludes>\n  </PropertyGroup>\n" +
        $"  <ItemGroup>\n    <ProjectReference Include=\"{contractsFolder}/{contractsFolder}.csproj\" />\n  </ItemGroup>\n";

    static PlannedArtifact Rewrite(PlannedArtifact artifact, Func<string, string> patch) =>
        PlannedArtifact.CreateText(artifact.RelativePath, patch(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources);

    static string InsertBefore(string text, string marker, string insertion)
    {
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal) >= 0)
        {
            throw UnsupportedSemanticRendering.For(nameof(EventContractsScaffold), $"scaffold marker '{marker.Trim()}'");
        }

        return text.Insert(index, insertion);
    }
}
