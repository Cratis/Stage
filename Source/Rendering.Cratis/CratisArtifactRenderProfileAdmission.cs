// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisArtifactRenderProfileAdmission
{
    const string ProjectInputPrefix = "cratis-scaffold:text:";

    public static bool Matches(ArtifactRenderRequest request, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
        => Matches(request.Profile, request.Model.Application.Name, request.Model, out admittedOptions, out mismatch);

    internal static bool MatchesScaffold(ArtifactRenderProfile profile, string applicationName, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
        => Matches(profile, applicationName, null, out admittedOptions, out mismatch);

    static bool Matches(ArtifactRenderProfile profile, string applicationName, ExecutableSemanticModel? model, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
    {
        admittedOptions = null;
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

        if (!TryGetApprovedPackages(profile, out var approvedPackages, out mismatch))
        {
            return false;
        }

        ArtifactRenderProfile expected;
        try
        {
            expected = CratisRendering.CreateProfile(applicationName, options!, null, null, null, approvedPackages);
        }
        catch (InvalidCratisBackendApplicationScaffold)
        {
            mismatch = "The Cratis profile contains invalid application, project, or root namespace options.";
            return false;
        }

        // A composed Scene is application content, so it is admitted separately rather than compared against the
        // package-owned roster - which is application-agnostic and could never contain one. Every scaffold input
        // is still matched byte for byte, and at most one Scene payload may accompany them.
        var scaffold = profile.Inputs.Where(_ => !IsComposedScene(_) && _.Name != StringsCatalogInput.Name && _.Name != AuthoringMetadataInput.Name && _.Name != DomainPlacementInput.Name && _.Name != Scene.ScreenPartitionsInput.Name && _.Name != ReducerConventionInput.Name && _.Name != RefusalModeInput.Name).ToArray();
        if (profile.Inputs.Count(IsComposedScene) > 1)
        {
            mismatch = "The Cratis profile carries more than one composed Scene payload.";
            return false;
        }

        var scene = profile.Inputs.SingleOrDefault(IsComposedScene);
        if (scene is not null && !Scene.SceneCompositionAdmission.Matches(scene, out mismatch))
        {
            return false;
        }

        if (!Scene.ScreenPartitionsInput.TryRead(profile, model, out _, out mismatch))
        {
            return false;
        }

        var strings = profile.Inputs.Where(_ => _.Name == StringsCatalogInput.Name).ToArray();
        if (strings.Length > 1 || (strings.Length == 1 && !StringsCatalogInput.TryRead(strings[0], out _)))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate strings catalog.";
            return false;
        }

        var domains = profile.Inputs.Where(input => input.Name == DomainPlacementInput.Name).ToArray();
        if (domains.Length > 1 || (domains.Length == 1 &&
            (!DomainPlacementInput.TryRead(domains[0], out var domain) || DomainPlacementInput.IsReserved(domain, expected))))
        {
            mismatch = "The Cratis profile carries invalid, reserved, or duplicate domain placement.";
            return false;
        }

        if (!ReducerConventionInput.TryRead(profile, applicationName, out _))
        {
            mismatch = "The Cratis profile carries malformed, stale, duplicate or unsupported reducer convention configuration.";
            return false;
        }

        var metadata = profile.Inputs.Where(input => input.Name == AuthoringMetadataInput.Name).ToArray();

        // Scaffold planning does not consume documentation and has no model against which to check its identities.
        // Source planning still validates it against the actual model before any semantic artifact is rendered.
        if (metadata.Length > 1 || (metadata.Length == 1 && model is not null && !AuthoringMetadataInput.TryRead(metadata[0], model, out _)))
        {
            mismatch = "The Cratis profile carries invalid, stale, or duplicate authoring metadata.";
            return false;
        }

        if (scaffold.Length != expected.Inputs.Length || !scaffold.Zip(expected.Inputs).All(InputMatches))
        {
            mismatch = "The Cratis profile scaffold roster, versions, bytes, or SHA-256 hashes do not match the package-owned profile.";
            return false;
        }

        var refusalInputs = profile.Inputs.Where(input => input.Name == RefusalModeInput.Name).ToArray();
        var refusals = ArtifactRefusalMode.Refuse;
        if (refusalInputs.Length > 1 || (refusalInputs.Length == 1 && !RefusalModeInput.TryRead(refusalInputs[0], out refusals)))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate refusal mode.";
            return false;
        }

        admittedOptions = options! with { Refusals = refusals };
        mismatch = string.Empty;
        return true;
    }

    // The approved package set is read first because the frontend that installs and loads those packages is generated
    // from it: the expected scaffold can only be regenerated once the document is known to be well formed.
    static bool TryGetApprovedPackages(ArtifactRenderProfile profile, out ApprovedPackageSet? approvedPackages, out string mismatch)
    {
        approvedPackages = null;
        mismatch = string.Empty;
        var inputs = profile.Inputs.Where(input => string.Equals(input.Name, $"{ProjectInputPrefix}{CratisFrontendApplicationScaffold.ApprovedPackagesDocument}", StringComparison.Ordinal)).ToArray();
        if (inputs.Length == 0)
        {
            return true;
        }

        try
        {
            var read = ApprovedPackageSetJson.Read(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()), CratisRendering.ShippedPackageNames);
            if (!read.Succeeded || read.Set!.IsEmpty)
            {
                mismatch = $"The Cratis profile carries an invalid approved package set: {string.Join(' ', read.Diagnostics.DefaultIfEmpty("it approves no package."))}";
                return false;
            }

            approvedPackages = read.Set;
            return true;
        }
        catch (DecoderFallbackException)
        {
            mismatch = "The Cratis profile carries an approved package set that is not valid UTF-8.";
            return false;
        }
    }

    static bool IsComposedScene(ArtifactRenderInput input) =>
        string.Equals(input.Name, $"{ProjectInputPrefix}{Scene.SceneCompositionInput.RelativePath}", StringComparison.Ordinal);

    static bool TryGetOptions(ArtifactRenderProfile profile, out CratisRenderingOptions? options)
    {
        options = null;
        var projectInputs = profile.Inputs
            .Where(input => input.Name.StartsWith(ProjectInputPrefix, StringComparison.Ordinal) &&
                            input.Name.EndsWith(".csproj", StringComparison.Ordinal) &&
                            !input.Name[ProjectInputPrefix.Length..].Contains('/') &&
                            !input.Name[ProjectInputPrefix.Length..].Contains('\\'))
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
