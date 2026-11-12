// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;
using PackageContract = Cratis.Screenplay.Extensions.ExtensionContract;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Checks what a package holds against the lock and against this Stage: the contract is the one the lock pins and is valid, everything it names is in the package,
/// every placeholder is static, and the requirements of the package are met.
/// </summary>
/// <param name="stageVersion">The version of this Stage.</param>
/// <param name="screenplayVersion">The version of Screenplay this Stage compiles with.</param>
public sealed class ExtensionPackageAdmission(string stageVersion, string screenplayVersion)
{
    /// <summary>
    /// Admits a package's contents.
    /// </summary>
    /// <param name="entry">The <see cref="ExtensionLockEntry"/>.</param>
    /// <param name="contents">The <see cref="PackageContents"/>.</param>
    /// <returns>The package's <see cref="PackageContract"/>.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the package must not be loaded.</exception>
    public PackageContract Admit(ExtensionLockEntry entry, PackageContents contents)
    {
        var bytes = contents.Contract ?? throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The package has no {PackageContents.ContractPath}.");
        var hash = ExtensionHashes.Of(bytes);
        if (hash != entry.ContractHash) throw new PackageRefusedException(PackageRefusalReason.ContractMismatch, $"The contract is {hash}, not the locked {entry.ContractHash}.");

        var parsed = ExtensionContractParser.Parse(ExtensionCache.Decode(bytes), PackageContents.ContractPath);
        var errors = parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ToList();
        if (parsed.Value is not { } contract || errors.Count > 0) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The contract is not valid: {string.Join(' ', errors)}");

        CheckFiles(contract, contents);
        CheckRequirements(contract);
        return contract;
    }

    static void CheckFiles(PackageContract contract, PackageContents contents)
    {
        if (contract.Icon is not null) Require("icon", contract.Icon, contents);
        foreach (var image in contract.Images) Require("image", image.Path, contents);
        if (contract.Frontend is not null) Require("frontend", contract.Frontend, contents);
        if (contract.Backend is not null && contents.Backend.Count == 0) throw new PackageRefusedException(PackageRefusalReason.Malformed, "The contract declares a backend but the package has nothing under backend/.");

        foreach (var component in contract.Components.Where(component => component.Placeholder is not null))
        {
            var html = Encoding.UTF8.GetString(Require("placeholder", component.Placeholder!, contents));
            var violations = ExtensionPlaceholder.Violations(html);
            if (violations.Count > 0) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The placeholder '{component.Placeholder}' {string.Join(", ", violations)}.");
        }
    }

    static byte[] Require(string kind, string path, PackageContents contents)
    {
        var normalized = path.StartsWith("./", StringComparison.Ordinal) ? path[2..] : path;
        if (normalized.Split('/').Any(PackageArchiveReader.IsUnsafeSegment) || normalized.Contains('\\')) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The {kind} '{path}' is outside the package.");
        return contents.Find(normalized) ?? throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The {kind} '{path}' is not in the package.");
    }

    string? OfferedFor(string kind)
    {
        if (string.Equals(kind, "stage", StringComparison.Ordinal)) return stageVersion;
        return string.Equals(kind, "screenplay", StringComparison.Ordinal) ? screenplayVersion : null;
    }

    void CheckRequirements(PackageContract contract)
    {
        foreach (var requirement in contract.Requirements)
        {
            var offered = OfferedFor(requirement.Kind);
            if (offered is not null && !ExtensionVersionRange.Satisfies(requirement.Value.Trim('"'), offered))
            {
                throw new PackageRefusedException(PackageRefusalReason.RequirementsNotMet, $"The package requires {requirement.Kind} {requirement.Value}, and this is {offered}.");
            }
        }
    }
}
