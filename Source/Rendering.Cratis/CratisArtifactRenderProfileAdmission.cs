// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisArtifactRenderProfileAdmission
{
    const string ProjectInputPrefix = "cratis-scaffold:text:";

    public static bool Matches(ArtifactRenderRequest request, out string mismatch)
    {
        var profile = request.Profile;
        if (!string.Equals(profile.Target, CratisRendering.TargetId, StringComparison.Ordinal) ||
            !string.Equals(profile.TargetVersion, CratisRendering.TargetVersion, StringComparison.Ordinal) ||
            !string.Equals(profile.Renderer, CratisRendering.RendererId, StringComparison.Ordinal) ||
            !string.Equals(profile.RendererVersion, CratisRendering.RendererVersion, StringComparison.Ordinal))
        {
            mismatch = $"Expected target '{CratisRendering.TargetId}' version '{CratisRendering.TargetVersion}' and renderer '{CratisRendering.RendererId}' version '{CratisRendering.RendererVersion}'.";
            return false;
        }

        if (!TryGetOptions(profile, out var options))
        {
            mismatch = "The Cratis profile does not contain one well-formed project input carrying explicit project and root namespace options.";
            return false;
        }

        ArtifactRenderProfile expected;
        try
        {
            expected = CratisRendering.CreateProfile(request.Model.Application.Name, options!);
        }
        catch (InvalidCratisBackendApplicationScaffold)
        {
            mismatch = "The Cratis profile contains invalid application, project, or root namespace options.";
            return false;
        }

        if (profile.Inputs.Length != expected.Inputs.Length || !profile.Inputs.Zip(expected.Inputs).All(InputMatches))
        {
            mismatch = "The Cratis profile scaffold roster, versions, bytes, or SHA-256 hashes do not match the package-owned profile.";
            return false;
        }

        mismatch = string.Empty;
        return true;
    }

    static bool TryGetOptions(ArtifactRenderProfile profile, out CratisRenderingOptions? options)
    {
        options = null;
        var projectInputs = profile.Inputs
            .Where(input => input.Name.StartsWith(ProjectInputPrefix, StringComparison.Ordinal) &&
                            input.Name.EndsWith(".csproj", StringComparison.Ordinal))
            .ToArray();
        if (projectInputs.Length != 1)
        {
            return false;
        }

        var relativePath = projectInputs[0].Name[ProjectInputPrefix.Length..];
        if (relativePath.Contains('/') || relativePath.Contains('\\'))
        {
            return false;
        }

        try
        {
            var project = XDocument.Parse(new UTF8Encoding(false, true).GetString(projectInputs[0].Bytes.AsSpan()));
            var rootNamespaces = project.Descendants("RootNamespace").Select(_ => _.Value).ToArray();
            if (rootNamespaces.Length != 1)
            {
                return false;
            }

            options = new(relativePath[..^".csproj".Length], rootNamespaces[0]);
            return true;
        }
        catch (Exception exception) when (exception is DecoderFallbackException or XmlException)
        {
            return false;
        }
    }

    static bool InputMatches((ArtifactRenderInput First, ArtifactRenderInput Second) pair) =>
        string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
        string.Equals(pair.First.Version, pair.Second.Version, StringComparison.Ordinal) &&
        string.Equals(pair.First.Sha256, pair.Second.Sha256, StringComparison.Ordinal) &&
        pair.First.Bytes.SequenceEqual(pair.Second.Bytes);
}
