// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PackageContract = Cratis.Screenplay.Extensions.ExtensionContract;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// How a loaded package describes itself to a person or a tool: its manifest and what it contains. The same shape is what a design tool and a marketplace read.
/// Settings are described by name and exposure only; a value never leaves the host.
/// </summary>
/// <param name="Id">The identifier of the package in addresses.</param>
/// <param name="Reference">The reference the model names.</param>
/// <param name="Resolved">The full reference it resolved to.</param>
/// <param name="Version">The locked version.</param>
/// <param name="Digest">The locked digest.</param>
/// <param name="ContractHash">The locked hash of the contract.</param>
/// <param name="Publisher">The publisher recorded in the lock.</param>
/// <param name="Channel">The channel it came from.</param>
/// <param name="SignatureVerified">Whether a signature by the publisher was verified.</param>
/// <param name="Name">The name the contract declares.</param>
/// <param name="DisplayName">The name a person sees.</param>
/// <param name="Description">What the package does.</param>
/// <param name="Icon">The address of the icon.</param>
/// <param name="Images">The marketplace images.</param>
/// <param name="Requirements">What the package requires.</param>
/// <param name="Contents">What the package contains.</param>
public sealed record ExtensionPackageDescription(
    string Id,
    string Reference,
    string Resolved,
    string Version,
    string Digest,
    string ContractHash,
    string Publisher,
    string Channel,
    bool SignatureVerified,
    string Name,
    string DisplayName,
    string Description,
    string? Icon,
    IReadOnlyList<PackageImage> Images,
    IReadOnlyList<PackageRequirement> Requirements,
    PackageContentsDescription Contents)
{
    /// <summary>
    /// The route prefix of the descriptions and of what is served for tooling.
    /// </summary>
    public const string Route = "/stage/extensions";

    /// <summary>
    /// Describes a loaded package.
    /// </summary>
    /// <param name="package">The <see cref="LoadedExtensionPackage"/>.</param>
    /// <returns>The <see cref="ExtensionPackageDescription"/>.</returns>
    public static ExtensionPackageDescription From(LoadedExtensionPackage package)
    {
        var contract = package.Contract;
        var prefix = $"{Route}/packages/{package.Id}";
        return new(
            package.Id,
            package.Lock.Reference,
            package.Lock.Resolved,
            package.Lock.Version,
            package.Digest,
            package.Lock.ContractHash,
            package.Lock.Publisher,
            package.Channel,
            package.SignatureVerified,
            contract.Name,
            contract.DisplayName ?? contract.Name,
            contract.Description ?? string.Empty,
            contract.Icon is null ? null : $"{prefix}/icon",
            [.. contract.Images.Select((image, index) => new PackageImage($"{prefix}/images/{index}", image.Caption))],
            [.. contract.Requirements.Select(requirement => new PackageRequirement(requirement.Kind, requirement.Value.Trim('"')))],
            new(
                [.. contract.Abilities.Select(ability => ability.Name)],
                [.. contract.Triggers.Select(trigger => trigger.Name)],
                [.. contract.Components.Select(component => new PackageComponent(component.Name, component.Kind, [.. component.Platforms], component.Placeholder is null ? null : $"{prefix}/placeholders/{Uri.EscapeDataString(component.Name)}"))],
                [.. contract.Settings.Select(setting => new PackageSetting(setting.Name, setting.Exposure.ToString().ToLowerInvariant()))],
                contract.Backend is null ? null : new PackageBackend(contract.Backend.Runtime, contract.Backend.Version),
                contract.Frontend is not null,
                ActivationOf(contract)));
    }

    static string? ActivationOf(PackageContract contract)
    {
        if (contract.Activation is null) return null;
        return contract.Activation.Always ? "always" : "on demand";
    }
}

/// <summary>A marketplace image.</summary>
/// <param name="Url">Where it is served.</param>
/// <param name="Caption">Its caption, when it has one.</param>
public sealed record PackageImage(string Url, string? Caption);

/// <summary>A requirement of a package.</summary>
/// <param name="Kind">Stage, screenplay, platform or runtime.</param>
/// <param name="Value">The range, platform or runtime it needs.</param>
public sealed record PackageRequirement(string Kind, string Value);

/// <summary>A component of a package.</summary>
/// <param name="Name">The component name.</param>
/// <param name="Kind">Its kind.</param>
/// <param name="Platforms">The platforms it supports.</param>
/// <param name="Placeholder">Where its placeholder is served, when it has one.</param>
public sealed record PackageComponent(string Name, string? Kind, IReadOnlyList<string> Platforms, string? Placeholder);

/// <summary>A setting of a package; never its value.</summary>
/// <param name="Name">The setting name.</param>
/// <param name="Exposure">Backend, public or secret.</param>
public sealed record PackageSetting(string Name, string Exposure);

/// <summary>The backend of a package.</summary>
/// <param name="Runtime">The runtime it needs.</param>
/// <param name="Version">The runtime version.</param>
public sealed record PackageBackend(string Runtime, string Version);

/// <summary>
/// What a package contains.
/// </summary>
/// <param name="Abilities">The abilities it offers.</param>
/// <param name="Triggers">The triggers it raises.</param>
/// <param name="Components">The components it ships.</param>
/// <param name="Settings">The settings it takes.</param>
/// <param name="Backend">The backend, or null for a package with none.</param>
/// <param name="HasFrontend">Whether it ships a frontend.</param>
/// <param name="Activation">The activation its manifest declares, or null.</param>
public sealed record PackageContentsDescription(
    IReadOnlyList<string> Abilities,
    IReadOnlyList<string> Triggers,
    IReadOnlyList<PackageComponent> Components,
    IReadOnlyList<PackageSetting> Settings,
    PackageBackend? Backend,
    bool HasFrontend,
    string? Activation);
