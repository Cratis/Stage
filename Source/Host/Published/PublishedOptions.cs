// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// The deployment configuration of a published host, bound from <c language="shell">Stage:Published</c>.
/// </summary>
/// <param name="PackagePath">The folder holding the exported application package, or a <c language="shell">.tar.gz</c> archive of it.</param>
/// <param name="StorageConnectionString">The connection string of the database the bundled Chronicle kernel stores its events and read models in.</param>
/// <param name="AuthenticationMode">How callers are authenticated.</param>
/// <param name="TrustedProxies">The addresses or CIDR networks whose forwarded headers and principal are trusted.</param>
/// <param name="AllowedHosts">The host names the application answers to.</param>
/// <param name="NotesEnabled">Whether the Notes backend is hosted, or <see langword="null"/> to follow the package manifest.</param>
/// <param name="ChronicleClientId">The Chronicle client id, or <see langword="null"/> for the kernel's built-in client.</param>
/// <param name="ChronicleClientSecret">The Chronicle client secret, or <see langword="null"/> for the kernel's built-in client.</param>
/// <param name="EventStore">The Chronicle event store name, or <see langword="null"/> to derive it from the published name.</param>
internal sealed record PublishedOptions(
    string PackagePath,
    string? StorageConnectionString,
    string? AuthenticationMode,
    IReadOnlyList<string> TrustedProxies,
    string? AllowedHosts,
    bool? NotesEnabled,
    string? ChronicleClientId,
    string? ChronicleClientSecret,
    string? EventStore = null)
{
    internal const string EventStorePrefix = "app-";

    internal const string DefaultPackagePath = "/stage/package";
    internal const string TrustedProxyMode = "TrustedProxy";
    internal const string OidcMode = "Oidc";
    internal const string Section = "Stage:Published";
    internal const string MongoDBStorage = "MongoDB";
    internal const string PostgreSqlStorage = "PostgreSql";
    internal const string MsSqlStorage = "MsSql";

    /// <summary>
    /// Gets the storage types the bundled Chronicle kernel can be started with, in the spelling the kernel documents.
    /// </summary>
    internal static readonly IReadOnlyList<string> StorageTypes = [MongoDBStorage, PostgreSqlStorage, MsSqlStorage];

    internal bool UsesOidc => string.Equals(AuthenticationMode, OidcMode, StringComparison.OrdinalIgnoreCase);
    internal bool UsesTrustedProxy => string.Equals(AuthenticationMode, TrustedProxyMode, StringComparison.OrdinalIgnoreCase);
    internal OidcPublishedOptions? Oidc { get; init; }

    /// <summary>
    /// Gets the configured storage type, as given, or <see langword="null"/> for the default (<c language="shell">MongoDB</c>).
    /// </summary>
    internal string? StorageType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime mounted the connection of an external Chronicle kernel
    /// (<c language="shell">CRATIS_CHRONICLE_CONNECTION_STRING_FILE</c>), so no kernel is bundled and no storage is configured here.
    /// </summary>
    internal bool UsesExternalKernel { get; init; }

    /// <summary>
    /// Gets the storage type in the kernel's spelling, or <see langword="null"/> when the configured one is not supported.
    /// </summary>
    internal string? CanonicalStorageType => string.IsNullOrWhiteSpace(StorageType)
        ? MongoDBStorage
        : StorageTypes.FirstOrDefault(type => string.Equals(type, StorageType.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether Chronicle stores in a SQL database (PostgreSQL or SQL Server).
    /// </summary>
    internal bool UsesSqlStorage => string.Equals(CanonicalStorageType, PostgreSqlStorage, StringComparison.Ordinal) || string.Equals(CanonicalStorageType, MsSqlStorage, StringComparison.Ordinal);

    /// <summary>
    /// Gets the sink the client registers read models with, so they land in the database the kernel stores in.
    /// </summary>
    internal SinkTypeId ReadModelSinkTypeId => UsesSqlStorage ? WellKnownSinkTypes.SQL : WellKnownSinkTypes.MongoDB;

    /// <summary>
    /// Gets a value indicating whether explicit Chronicle credentials are configured.
    /// </summary>
    internal bool HasChronicleCredentials => ChronicleClientId is { Length: > 0 } && ChronicleClientSecret is { Length: > 0 };

    /// <summary>
    /// Reads the options. Secrets can be given directly or as the path of a mounted secret file.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="environment">Reads an environment variable, or <see langword="null"/> for the process environment.</param>
    /// <returns>The options, not yet validated.</returns>
    internal static PublishedOptions From(IConfiguration configuration, Func<string, string?>? environment = null)
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
            Secret(published, "Chronicle:ClientSecret"),
            published["EventStore"] is { Length: > 0 } eventStore ? eventStore.Trim() : null)
        {
            Oidc = new OidcPublishedOptions(configuration),
            StorageType = published["Storage:Type"],
            UsesExternalKernel = SandboxRuntimeConventions.HasMountedConnectionString(environment ?? Environment.GetEnvironmentVariable)
        };
    }

    /// <summary>
    /// Gets the Chronicle event store of the application. Published applications share one storage server, so each one's
    /// data is isolated by this name: it is never random, and survives restarts, redeploys and new versions.
    /// </summary>
    /// <param name="publishedName">The host label the application is published under, from the package manifest.</param>
    /// <returns>The configured name, otherwise the published name prefixed with "app-".</returns>
    internal string EventStoreFor(string publishedName) => EventStore is { Length: > 0 } configured ? configured : EventStorePrefix + publishedName;

    /// <summary>
    /// Lists everything that stops this configuration from running a published application.
    /// </summary>
    /// <returns>The problems, empty when the configuration is complete.</returns>
    internal IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();
        if (CanonicalStorageType is null)
        {
            problems.Add($"Storage type '{StorageType}' is not supported. Set {Section}:Storage:Type to one of {string.Join(", ", StorageTypes)}.");
        }

        if (!UsesExternalKernel && string.IsNullOrWhiteSpace(StorageConnectionString))
        {
            problems.Add($"No persistent storage is configured. Set {Section}:Storage:ConnectionString (or {Section}:Storage:ConnectionStringFile to a mounted secret) to a connection string for the {Section}:Storage:Type database ({CanonicalStorageType ?? MongoDBStorage} by default), or mount the connection of an external Chronicle kernel with {SandboxRuntimeConventions.ConnectionStringFile}.");
        }

        if (string.IsNullOrWhiteSpace(AuthenticationMode))
        {
            problems.Add($"No authentication is configured. Set {Section}:Authentication:Mode to {TrustedProxyMode}; a published application never runs without sign-in.");
        }
        else if (!UsesTrustedProxy && !UsesOidc)
        {
            problems.Add($"Authentication mode '{AuthenticationMode}' is not supported. Supported modes are {TrustedProxyMode} and Oidc.");
        }

        if (UsesOidc)
        {
            problems.AddRange(Oidc?.Problems() ?? ["Oidc authentication configuration is missing."]);
        }

        if (!UsesOidc && TrustedProxies.Count == 0)
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
