// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Stage.Publishing;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Loads the packages a model's lock pins when the host starts.
/// </summary>
public static class ExtensionPackageHosting
{
    /// <summary>
    /// Loads the packages the model's <c lang="text">screenplay.lock</c> pins. A model with no lock, or a lock that pins nothing, loads nothing and needs no configuration.
    /// </summary>
    /// <param name="modelPath">The model's folder or one of its files.</param>
    /// <param name="configuration">The configuration, whose <c lang="text">Stage:Extensions:Packages</c> section holds the <see cref="ExtensionPackageOptions"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="cancellationToken">Cancelled when startup stops.</param>
    /// <returns>The <see cref="ExtensionPackageSet"/>. The caller makes its <see cref="ExtensionPackageSet.Contracts"/> the compiler's by setting <see cref="ExtensionContractResolver.Current"/>.</returns>
    public static async Task<ExtensionPackageSet> Load(string modelPath, IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken = default)
    {
        var anchor = Path.Combine(Directory.Exists(modelPath) ? Path.GetFullPath(modelPath) : Path.GetDirectoryName(Path.GetFullPath(modelPath))!, "model.play");
        if (ExtensionContractResolver.FindLock([anchor]) is not { } lockPath) return ExtensionPackageSet.Empty;

        var parsed = ExtensionLock.Parse(File.ReadAllText(lockPath), lockPath);
        if (parsed.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new([], [new(ExtensionLock.FileName, PackageRefusalReason.Malformed, string.Join(' ', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)))]);
        }

        if (parsed.Value is not { Entries.Length: > 0 } extensionLock) return ExtensionPackageSet.Empty;

        var options = configuration.GetSection(ExtensionPackageOptions.Section).Get<ExtensionPackageOptions>() ?? new();
        using var http = new HttpClient { Timeout = options.RequestTimeout };
        var root = options.CacheDirectory.Length > 0 ? options.CacheDirectory : ExtensionPackageCache.DefaultRoot;
        var registry = new OciRegistryClient(http, options);
        var loader = new ExtensionPackageLoader(
            [new OciPackageChannel(registry, options), new NpmPackageChannel(http, options)],
            new(root),
            new(options, ProjectExtensionPolicy.Read(modelPath)),
            new(Versions.Stage, Versions.Screenplay),
            options,
            new(Path.Combine(root, "contracts")),
            loggerFactory.CreateLogger<ExtensionPackageLoader>());

        return await loader.Load(extensionLock, cancellationToken);
    }

    static class Versions
    {
        public static string Stage { get; } = StageRuntimeVersion.Current;

        public static string Screenplay { get; } = typeof(ExtensionLock).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(ExtensionLock).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
