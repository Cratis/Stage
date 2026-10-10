// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class DomainPlacementInput
{
    internal const string Name = "cratis-domain-placement";
    const string Version = "1";
    static readonly string[] _reservedFolders = ["Customizations", "GeneratedPolicies", "TypedContexts", "GeneratedCommands", "GeneratedTenancy"];

    internal static ArtifactRenderProfile WithDomain(ArtifactRenderProfile profile, string domain) => domain.Length == 0 ? profile :
        ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, profile.Inputs.Add(ArtifactRenderInput.Create(Name, Version, [.. Encoding.UTF8.GetBytes(domain)])));

    internal static bool TryRead(ArtifactRenderInput input, out string domain)
    {
        domain = Encoding.UTF8.GetString(input.Bytes.AsSpan());
        return input.Version == Version && TryNormalize(domain, out var normalized) && normalized == domain;
    }

    internal static ImmutableArray<string> From(ArtifactRenderProfile profile)
    {
        var input = profile.Inputs.SingleOrDefault(input => input.Name == Name);
        return input is null ? [] : [.. Encoding.UTF8.GetString(input.Bytes.AsSpan()).Split('/')];
    }

    internal static bool TryNormalize(string domain, out string normalized)
    {
        normalized = string.Empty;
        if (domain.Length == 0) return true;
        var segments = domain.Replace('\\', '/').Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment != segment.Trim() ||
            segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ' ' and not '-' and not '_')))
        {
            return false;
        }
        var names = segments.Select(Identifiers.ToPascalCase).ToArray();
        if (names.Any(name => name.Length == 0 || (!char.IsAsciiLetter(name[0]) && name[0] != '_') ||
            name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_')))
        {
            return false;
        }
        normalized = string.Join('/', names);

        return true;
    }

    internal static bool IsReserved(string domain, ArtifactRenderProfile profile)
    {
        if (domain.Length == 0) return false;
        var first = domain.Split('/')[0];
        var reserved = _reservedFolders
            .Concat(profile.Inputs.Select(input =>
                CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) ? artifact!.RelativePath.Split('/')[0] : string.Empty));

        return reserved.Contains(first, StringComparer.OrdinalIgnoreCase);
    }
}
