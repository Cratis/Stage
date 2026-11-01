// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

/// <summary>
/// The deployment configuration of a published host, bound from <c language="shell">Stage:Published</c>.
/// </summary>
/// <param name="PackagePath">The folder holding the exported application package.</param>
/// <param name="StorageConnectionString">The MongoDB connection string for Chronicle and its read models.</param>
/// <param name="AuthenticationMode">How callers are authenticated.</param>
/// <param name="TrustedProxies">The addresses or CIDR networks whose forwarded headers and principal are trusted.</param>
/// <param name="AllowedHosts">The host names the application answers to.</param>
/// <param name="NotesEnabled">Whether the Notes backend is hosted, or <see langword="null"/> to follow the package manifest.</param>
/// <param name="ChronicleClientId">The Chronicle client id, or <see langword="null"/> for the kernel's built-in client.</param>
/// <param name="ChronicleClientSecret">The Chronicle client secret, or <see langword="null"/> for the kernel's built-in client.</param>
internal sealed record PublishedOptions(
    string PackagePath,
    string? StorageConnectionString,
    string? AuthenticationMode,
    IReadOnlyList<string> TrustedProxies,
    string? AllowedHosts,
    bool? NotesEnabled,
    string? ChronicleClientId,
    string? ChronicleClientSecret)
{
    internal const string DefaultPackagePath = "/stage/package";
    internal const string TrustedProxyMode = "TrustedProxy";
    internal const string Section = "Stage:Published";

    /// <summary>
    /// Gets a value indicating whether explicit Chronicle credentials are configured.
    /// </summary>
    internal bool HasChronicleCredentials => ChronicleClientId is { Length: > 0 } && ChronicleClientSecret is { Length: > 0 };

    /// <summary>
    /// Reads the options. Secrets can be given directly or as the path of a mounted secret file.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The options, not yet validated.</returns>
    internal static PublishedOptions From(IConfiguration configuration)
    {
        var published = configuration.GetSection(Section);
        var proxies = published.GetSection("TrustedProxies").Get<string[]>() ?? [];
        return new PublishedOptions(
            published["PackagePath"] is { Length: > 0 } path ? path : DefaultPackagePath,
            Secret(published, "Storage:ConnectionString"),
            published["Authentication:Mode"],
            [.. proxies.Where(proxy => !string.IsNullOrWhiteSpace(proxy)).Select(proxy => proxy.Trim())],
            configuration["AllowedHosts"],
            bool.TryParse(published["Notes:Enabled"], out var enabled) ? enabled : null,
            published["Chronicle:ClientId"],
            Secret(published, "Chronicle:ClientSecret"));
    }

    /// <summary>
    /// Lists everything that stops this configuration from running a published application.
    /// </summary>
    /// <returns>The problems, empty when the configuration is complete.</returns>
    internal IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(StorageConnectionString))
        {
            problems.Add($"No persistent storage is configured. Set {Section}:Storage:ConnectionString (or {Section}:Storage:ConnectionStringFile to a mounted secret) to a MongoDB connection string.");
        }

        if (string.IsNullOrWhiteSpace(AuthenticationMode))
        {
            problems.Add($"No authentication is configured. Set {Section}:Authentication:Mode to {TrustedProxyMode}; a published application never runs without sign-in.");
        }
        else if (!string.Equals(AuthenticationMode, TrustedProxyMode, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"Authentication mode '{AuthenticationMode}' is not supported. The only supported mode is {TrustedProxyMode}.");
        }

        if (TrustedProxies.Count == 0)
        {
            problems.Add($"No trusted proxy is configured. Set {Section}:TrustedProxies to the address or CIDR network of the sign-in proxy (for a proxy in the same pod, 127.0.0.1 and ::1).");
        }

        foreach (var proxy in TrustedProxies.Where(proxy => !TrustedProxyNetwork.TryParse(proxy, out _)))
        {
            problems.Add($"Trusted proxy '{proxy}' is not an IP address or CIDR network.");
        }

        if (string.IsNullOrWhiteSpace(AllowedHosts) || AllowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains("*"))
        {
            problems.Add("AllowedHosts must list the published host names (for example my-app.cratis.app), not '*' and not empty.");
        }

        if ((ChronicleClientId is { Length: > 0 }) != (ChronicleClientSecret is { Length: > 0 }))
        {
            problems.Add($"Set both {Section}:Chronicle:ClientId and {Section}:Chronicle:ClientSecret, or neither.");
        }

        return problems;
    }

    /// <summary>
    /// Throws when the configuration is incomplete.
    /// </summary>
    /// <exception cref="PublishedModeRefused">Something required is missing.</exception>
    internal void EnsureComplete()
    {
        var problems = Problems();
        if (problems.Count > 0)
        {
            throw new PublishedModeRefused(problems);
        }
    }

    /// <summary>
    /// Parses the trusted proxies, skipping any that are malformed.
    /// </summary>
    /// <returns>The networks.</returns>
    internal IReadOnlyList<TrustedProxyNetwork> Networks() =>
        [.. TrustedProxies.Select(proxy => TrustedProxyNetwork.TryParse(proxy, out var network) ? network : null).OfType<TrustedProxyNetwork>()];

    static string? Secret(IConfigurationSection section, string key)
    {
        if (section[key] is { Length: > 0 } direct)
        {
            return direct;
        }

        return section[$"{key}File"] is { Length: > 0 } file && File.Exists(file) ? File.ReadAllText(file).Trim() : null;
    }
}
