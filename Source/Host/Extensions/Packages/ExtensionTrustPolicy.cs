// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// What trust allows for one locked package.
/// </summary>
/// <param name="Allowed">Whether the package may be loaded at all.</param>
/// <param name="Refusal">Why it may not, or empty.</param>
/// <param name="Keys">The keys the package's signature must verify against.</param>
/// <param name="RequireSignature">Whether the package must be signed.</param>
public sealed record PackageTrust(bool Allowed, string Refusal, IReadOnlyList<string> Keys, bool RequireSignature);

/// <summary>
/// Decides whether a locked package is allowed. Naming a package in a model, or a name in the lock, is never a decision: the deployment and the project are.
/// A package is allowed when everything either of them lists matches it, and at least one of them lists something. Publishers and references are both
/// matched when listed, so an allowed publisher can be limited to the references wanted from it.
/// </summary>
/// <param name="deployment">The <see cref="ExtensionPackageOptions"/> of the deployment.</param>
/// <param name="project">The <see cref="ProjectExtensionPolicy"/> of the project.</param>
public sealed class ExtensionTrustPolicy(ExtensionPackageOptions deployment, ProjectExtensionPolicy project)
{
    /// <summary>
    /// Decides for one locked entry.
    /// </summary>
    /// <param name="entry">The <see cref="ExtensionLockEntry"/>.</param>
    /// <returns>The <see cref="PackageTrust"/>.</returns>
    public PackageTrust Evaluate(ExtensionLockEntry entry)
    {
        var keys = KeysOf(entry.Publisher);
        var signatures = deployment.RequireSignatures;

        if (project.Error.Length > 0) return Refuse(project.Error, signatures);
        if (deployment.Allow.IsEmpty && project.Allow.IsEmpty)
        {
            return Refuse($"Nothing allows extension packages: list publishers or references in {ProjectExtensionPolicy.RelativePath} or under {ExtensionPackageOptions.Section}:Allow.", signatures);
        }

        foreach (var (source, allow) in new[] { ("this deployment", deployment.Allow), ("the project", project.Allow) })
        {
            if (allow.IsEmpty) continue;
            if (allow.Publishers.Length > 0 && !Matches(allow.Publishers, entry.Publisher)) return Refuse($"The publisher '{entry.Publisher}' is not allowed by {source}.", signatures);
            if (allow.References.Length > 0 && !Matches(allow.References, entry.Resolved)) return Refuse($"The package '{entry.Resolved}' is not allowed by {source}.", signatures);
        }

        if (signatures && keys.Length == 0) return Refuse($"No signing key is configured for the publisher '{entry.Publisher}', so its signature cannot be verified.", signatures);

        return new(true, string.Empty, keys, signatures);
    }

    static PackageTrust Refuse(string reason, bool signatures) => new(false, reason, [], signatures);

    static bool Matches(string[] patterns, string value) => patterns.Any(pattern => pattern.EndsWith('*')
        ? value.StartsWith(pattern[..^1], StringComparison.Ordinal)
        : string.Equals(pattern, value, StringComparison.Ordinal));

    string[] KeysOf(string publisher)
    {
        if (deployment.Publishers.TryGetValue(publisher, out var configured) && configured.Keys.Length > 0) return configured.Keys;
        return project.Publishers.TryGetValue(publisher, out var listed) ? listed.Keys : [];
    }
}
