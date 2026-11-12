// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Configuration of extension package loading, bound to <c lang="text">Stage:Extensions:Packages</c>. Naming a package in a model never trusts it:
/// nothing is loaded unless the deployment or the project's <c lang="text">.cratis/extensions.json</c> allows it.
/// </summary>
public sealed class ExtensionPackageOptions
{
    /// <summary>
    /// Gets the configuration section.
    /// </summary>
    public const string Section = "Stage:Extensions:Packages";

    /// <summary>
    /// Gets or sets the folder packages are cached in, by digest. Empty means <c lang="text">Cratis/Stage/extensions</c> in the local application data folder.
    /// </summary>
    public string CacheDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what this deployment allows. It narrows what the project allows: a package must be allowed by both when both say something.
    /// </summary>
    public PackageAllowOptions Allow { get; set; } = new();

    /// <summary>
    /// Gets or sets the signing keys by publisher identity, as the publisher recorded in the lock. A key is a PEM public key, or its base64 body, for ECDSA P-256.
    /// Keys here replace the keys the project lists for the same publisher.
    /// </summary>
    public IDictionary<string, PublisherOptions> Publishers { get; set; } = new Dictionary<string, PublisherOptions>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets whether a package must carry a valid signature by its publisher. Only the deployment can turn this off, and it is meant for local development.
    /// </summary>
    public bool RequireSignatures { get; set; } = true;

    /// <summary>
    /// Gets or sets the credentials for private registries by registry host, such as <c lang="text">ghcr.io</c>. They come from configuration only, never from the model or the lock.
    /// </summary>
    public IDictionary<string, RegistryCredentials> Registries { get; set; } = new Dictionary<string, RegistryCredentials>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the npm registry frontend-only packages are read from.
    /// </summary>
    public string NpmRegistry { get; set; } = "https://registry.npmjs.org";

    /// <summary>
    /// Gets or sets the most bytes a package may unpack to. Anything larger is refused.
    /// </summary>
    public long MaxPackageBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>
    /// Gets or sets the most files a package may hold.
    /// </summary>
    public int MaxPackageFiles { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets how long one request to a registry may take.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
}

/// <summary>
/// What a deployment or a project allows.
/// </summary>
public sealed class PackageAllowOptions
{
    /// <summary>
    /// Gets or sets the publisher identities that are allowed. A trailing <c lang="text">*</c> matches a prefix.
    /// </summary>
    public string[] Publishers { get; set; } = [];

    /// <summary>
    /// Gets or sets the resolved references that are allowed, such as <c lang="text">ghcr.io/cratis/extension-mail</c>. A trailing <c lang="text">*</c> matches a prefix.
    /// </summary>
    public string[] References { get; set; } = [];

    /// <summary>
    /// Gets whether anything is listed.
    /// </summary>
    public bool IsEmpty => Publishers.Length == 0 && References.Length == 0;
}

/// <summary>
/// The signing keys of one publisher.
/// </summary>
public sealed class PublisherOptions
{
    /// <summary>
    /// Gets or sets the ECDSA P-256 public keys the publisher signs with. More than one allows a key to be rotated.
    /// </summary>
    public string[] Keys { get; set; } = [];
}

/// <summary>
/// The credentials for one private registry.
/// </summary>
public sealed class RegistryCredentials
{
    /// <summary>
    /// Gets or sets the user name for basic authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password or access token.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
