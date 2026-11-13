// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
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
    internal const string ProjectInputPrefix = "cratis-scaffold:text:";

    public static bool Matches(ArtifactRenderRequest request, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
        => CratisRenderProfileVersions.MatchesAnyKnownVersion((v, out o, out m) => Matches(request, v, out o, out m), out admittedOptions, out mismatch);

    internal static bool Matches(ArtifactRenderRequest request, string? profileVersion, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
        => Matches(new ProfileAdmissionTarget(request.Profile, request.Model.Application.Name, request.Model, profileVersion), out admittedOptions, out mismatch);

    internal static bool MatchesScaffold(ArtifactRenderProfile profile, string applicationName, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
        => CratisRenderProfileVersions.MatchesAnyKnownVersion((v, out o, out m) => Matches(new ProfileAdmissionTarget(profile, applicationName, null, v), out o, out m), out admittedOptions, out mismatch);

    internal static bool Matches(ProfileAdmissionTarget target, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
    {
        admittedOptions = null;
        var profile = target.Profile;
        if (!IsCratisTargetAndRenderer(profile))
        {
            mismatch = $"Expected target '{CratisRendering.TargetId}' version '{CratisRendering.TargetVersion}' and renderer '{CratisRendering.RendererId}' version '{CratisRendering.RendererVersion}'.";
            return false;
        }

        if (!TryGetOptions(profile, out var options))
        {
            mismatch = "The Cratis profile does not contain one well-formed project input carrying explicit project and root namespace options.";
            return false;
        }

        if (!TryAdmitExpectedProfile(target, options!, out var expected, out mismatch))
        {
            return false;
        }

        if (!ScaffoldRosterMatches(profile, expected!))
        {
            mismatch = "The Cratis profile scaffold roster, versions, bytes, or SHA-256 hashes do not match the package-owned profile.";
            return false;
        }

        return TryAdmitStrictOptions(profile, options!, expected!, out var strictOptions, out mismatch) &&
            TryAdmitServiceLayout(profile, strictOptions!, out admittedOptions, out mismatch);
    }

    internal static bool TryGetOptions(ArtifactRenderProfile profile, out CratisRenderingOptions? options)
    {
        options = null;
        var projectInputs = profile.Inputs
            .Where(input => input.Name.StartsWith(ProjectInputPrefix, StringComparison.Ordinal) &&
                            input.Name.EndsWith(".csproj", StringComparison.Ordinal) &&
                            !ContainsSeparator(input.Name[ProjectInputPrefix.Length..]))
            .ToArray();
        if (projectInputs.Length != 1)
        {
            return false;
        }

        try
        {
            options = ReadOptions(profile, projectInputs[0]);
            return options is not null;
        }
        catch (Exception exception) when (exception is DecoderFallbackException or XmlException)
        {
            return false;
        }
    }

    static bool TryAdmitExpectedProfile(ProfileAdmissionTarget target, CratisRenderingOptions options, out ArtifactRenderProfile? expected, out string mismatch)
    {
        var profile = target.Profile;
        expected = null;
        if (!TryGetApprovedPackages(profile, out var approvedPackages, out mismatch) ||
            !CratisRenderProfileVersions.TryResolve(profile, target.ProfileVersion, out var scaffoldProfile, out mismatch))
        {
            return false;
        }

        return TryCreateExpected(target, options, approvedPackages, scaffoldProfile, out expected, out mismatch) &&
            TryAdmitScene(profile, target.Model, out mismatch) &&
            TryAdmitCatalogs(profile, target.Model, out mismatch) &&
            TryAdmitPlacementAndEvidence(target, expected!, out mismatch);
    }

    // A composed Scene is application content, so it is admitted separately rather than compared against the
    // package-owned roster - which is application-agnostic and could never contain one. Every scaffold input
    // is still matched byte for byte, and at most one Scene payload may accompany them.
    static bool ScaffoldRosterMatches(ArtifactRenderProfile profile, ArtifactRenderProfile expected)
    {
        var scaffold = profile.Inputs.Where(input => !IsComposedScene(input) && !CratisScaffoldProtocolInputs.Contains(input.Name)).ToArray();
        return scaffold.Length == expected.Inputs.Length && scaffold.Zip(expected.Inputs).All(InputMatches);
    }

    static bool ContainsSeparator(string path) => path.Contains('/') || path.Contains('\\');

    static CratisRenderingOptions? ReadOptions(ArtifactRenderProfile profile, ArtifactRenderInput projectInput)
    {
        var project = XDocument.Parse(new UTF8Encoding(false, true).GetString(projectInput.Bytes.AsSpan()));
        var rootNamespaces = project.Descendants("RootNamespace").Select(_ => _.Value).ToArray();
        if (rootNamespaces.Length != 1)
        {
            return null;
        }

        var relativePath = projectInput.Name[ProjectInputPrefix.Length..];
        return new(relativePath[..^".csproj".Length], rootNamespaces[0])
        {
            RepositoryRoot = profile.Inputs.Any(input => input.Name.StartsWith($"{ProjectInputPrefix}{CratisContinuousIntegration.Directory}", StringComparison.Ordinal))
        };
    }

    static bool IsCratisTargetAndRenderer(ArtifactRenderProfile profile) =>
        string.Equals(profile.Target, CratisRendering.TargetId, StringComparison.Ordinal) &&
        string.Equals(profile.TargetVersion, CratisRendering.TargetVersion, StringComparison.Ordinal) &&
        string.Equals(profile.Renderer, CratisRendering.RendererId, StringComparison.Ordinal) &&
        string.Equals(profile.RendererVersion, CratisRendering.RendererVersion, StringComparison.Ordinal);

    static bool TryCreateExpected(ProfileAdmissionTarget target, CratisRenderingOptions options, ApprovedPackageSet? approvedPackages, CratisBackendApplicationScaffoldProfile scaffoldProfile, out ArtifactRenderProfile? expected, out string mismatch)
    {
        expected = null;
        mismatch = string.Empty;
        try
        {
            expected = CratisRendering.CreateProfile(target.ApplicationName, options, null, null, null, approvedPackages, ListsPrimeReact(target.Profile), scaffoldProfile);
            return true;
        }
        catch (InvalidCratisBackendApplicationScaffold)
        {
            mismatch = "The Cratis profile contains invalid application, project, or root namespace options.";
            return false;
        }
    }

    static bool TryAdmitScene(ArtifactRenderProfile profile, ExecutableSemanticModel? model, out string mismatch)
    {
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

        return Scene.ScreenPartitionsInput.TryRead(profile, model, out _, out mismatch);
    }

    static bool TryAdmitCatalogs(ArtifactRenderProfile profile, ExecutableSemanticModel? model, out string mismatch)
    {
        mismatch = string.Empty;
        var strings = profile.Inputs.Where(_ => _.Name == StringsCatalogInput.Name).ToArray();
        if (strings.Length > 1 || (strings.Length == 1 && !StringsCatalogInput.TryRead(strings[0], out _)))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate strings catalog.";
            return false;
        }

        if (!TryReadRealization(profile, out mismatch))
        {
            return false;
        }

        if (!PassiveReadModelsInput.TryRead(profile, model, out _))
        {
            mismatch = "The Cratis profile carries malformed, stale, duplicate or unsupported passive read-model metadata.";
            return false;
        }

        if (!Realization.SinksInput.TryRead(profile, model, out _))
        {
            mismatch = "The Cratis profile carries malformed, stale, duplicate or inconsistent sink metadata.";
            return false;
        }

        return true;
    }

    static bool TryAdmitPlacementAndEvidence(ProfileAdmissionTarget target, ArtifactRenderProfile expected, out string mismatch)
    {
        mismatch = string.Empty;
        if (!HasValidDomainPlacement(target.Profile, expected))
        {
            mismatch = "The Cratis profile carries invalid, reserved, or duplicate domain placement.";
            return false;
        }

        return TryAdmitEvidence(target, out mismatch);
    }

    static bool HasValidDomainPlacement(ArtifactRenderProfile profile, ArtifactRenderProfile expected)
    {
        var domains = profile.Inputs.Where(input => input.Name == DomainPlacementInput.Name).ToArray();
        return domains.Length switch
        {
            0 => true,
            1 => DomainPlacementInput.TryRead(domains[0], out var domain) && !DomainPlacementInput.IsReserved(domain, expected),
            _ => false
        };
    }

    static bool HasValidAuthoringMetadata(ArtifactRenderProfile profile, ExecutableSemanticModel? model)
    {
        var metadata = profile.Inputs.Where(input => input.Name == AuthoringMetadataInput.Name).ToArray();

        // Scaffold planning does not consume documentation and has no model against which to check its identities.
        // Source planning still validates it against the actual model before any semantic artifact is rendered.
        return metadata.Length switch
        {
            0 => true,
            1 => model is null || AuthoringMetadataInput.TryRead(metadata[0], model, out _),
            _ => false
        };
    }

    static bool TryAdmitEvidence(ProfileAdmissionTarget target, out string mismatch)
    {
        var profile = target.Profile;
        mismatch = string.Empty;
        if (!ReducerConventionInput.TryRead(profile, target.ApplicationName, out _))
        {
            mismatch = "The Cratis profile carries malformed, stale, duplicate or unsupported reducer convention configuration.";
            return false;
        }

        if (!ReducerMigrationInput.TryRead(profile, out _))
        {
            mismatch = "The Cratis profile carries malformed or duplicate reducer migration evidence.";
            return false;
        }

        if (!HasValidAuthoringMetadata(profile, target.Model))
        {
            mismatch = "The Cratis profile carries invalid, stale, or duplicate authoring metadata.";
            return false;
        }

        if (!SourceLineMappingInput.TryRead(profile, out _))
        {
            mismatch = "The Cratis profile carries invalid or duplicate source line mapping.";
            return false;
        }

        return true;
    }

    static bool TryAdmitStrictOptions(ArtifactRenderProfile profile, CratisRenderingOptions options, ArtifactRenderProfile expected, out CratisRenderingOptions? strictOptions, out string mismatch)
    {
        strictOptions = null;
        mismatch = string.Empty;
        if (!TryReadRefusalMode(profile, out var refusals))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate refusal mode.";
            return false;
        }

        // Header admission belongs to the final artifact pipeline, including malformed inputs.
        if (!ArtifactFileHeader.TryRead(profile, out var fileHeader)) fileHeader = [];

        if (!UserInterfaceInput.TryRead(profile, out var userInterface))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate user interface input.";
            return false;
        }

        if (!HasValidUserInterfaceDomains(profile, userInterface, expected))
        {
            mismatch = "The Cratis profile carries an invalid, duplicate, reserved or unaccompanied user interface domains input.";
            return false;
        }

        return CratisStrictProfileInput.TryAdmit(profile, options with { Refusals = refusals, FileHeader = fileHeader }, out strictOptions, out mismatch);
    }

    static bool TryReadRefusalMode(ArtifactRenderProfile profile, out ArtifactRefusalMode refusals)
    {
        var refusalInputs = profile.Inputs.Where(input => input.Name == RefusalModeInput.Name).ToArray();
        refusals = ArtifactRefusalMode.Refuse;
        return refusalInputs.Length switch
        {
            0 => true,
            1 => RefusalModeInput.TryRead(refusalInputs[0], out refusals),
            _ => false
        };
    }

    static bool HasValidUserInterfaceDomains(ArtifactRenderProfile profile, object? userInterface, ArtifactRenderProfile expected)
    {
        if (!UserInterfaceDomainsInput.TryRead(profile, out var userInterfaceDomains))
        {
            return false;
        }

        return userInterfaceDomains is not { } named ||
            (userInterface is not null && !named.Keys.Any(domain => DomainPlacementInput.IsReserved(domain, expected)));
    }

    // The application realization and how the realized application trusts the identity AuthProxy forwards.
    static bool TryReadRealization(ArtifactRenderProfile profile, out string mismatch)
    {
        mismatch = string.Empty;
        if (!Realization.ApplicationRealizationInput.TryRead(profile, out _))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate application realization.";
            return false;
        }

        if (!SignedForwardedIdentityInput.TryRead(profile, out _))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate signed forwarded identity input.";
            return false;
        }

        return true;
    }

    static bool TryAdmitServiceLayout(ArtifactRenderProfile profile, CratisRenderingOptions options, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
    {
        admittedOptions = null;
        mismatch = string.Empty;
        if (!ServiceLayoutInput.TryAdmit(profile, options, out var serviceLayout))
        {
            mismatch = "The Cratis profile carries an invalid or duplicate service layout input, or combines it with the repository root.";
            return false;
        }

        admittedOptions = options with { ServiceLayout = serviceLayout, SignedForwardedIdentity = SignedForwardedIdentityInput.IsCarriedBy(profile) };
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

    // The composed Scene is application content, so whether the application opted in to PrimeReact is read from the ui profiles it declares.
    static bool ListsPrimeReact(ArtifactRenderProfile profile)
    {
        var scene = profile.Inputs.FirstOrDefault(IsComposedScene);
        if (scene is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(scene.Bytes.AsMemory());
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("uiProfiles", out var profiles) &&
                profiles.ValueKind == JsonValueKind.Array &&
                CratisRendering.ListsPrimeReact(profiles.EnumerateArray()
                    .Where(uiProfile => uiProfile.ValueKind == JsonValueKind.Object && uiProfile.TryGetProperty("packages", out var packages) && packages.ValueKind == JsonValueKind.Array)
                    .SelectMany(uiProfile => uiProfile.GetProperty("packages").EnumerateArray())
                    .Where(package => package.ValueKind == JsonValueKind.String)
                    .Select(package => package.GetString()!));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool IsComposedScene(ArtifactRenderInput input) =>
        string.Equals(input.Name, $"{ProjectInputPrefix}{Scene.SceneCompositionInput.RelativePath}", StringComparison.Ordinal);

    static bool InputMatches((ArtifactRenderInput First, ArtifactRenderInput Second) pair) =>
        string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
        string.Equals(pair.First.Version, pair.Second.Version, StringComparison.Ordinal) &&
        string.Equals(pair.First.Sha256, pair.Second.Sha256, StringComparison.Ordinal) &&
        pair.First.Bytes.SequenceEqual(pair.Second.Bytes);
}
