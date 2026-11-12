// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Packages;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Unpacks a package's backend to run it. Each start unpacks anew, from bytes verified against the locked digest again, so a folder a previous run left changed is never what runs.
/// </summary>
public static class BackendFolders
{
    /// <summary>
    /// Unpacks the backend of a package.
    /// </summary>
    /// <param name="launch">The <see cref="BackendLaunch"/>.</param>
    /// <param name="options">The <see cref="ExecutionOptions"/>.</param>
    /// <returns>The folder the backend is unpacked to.</returns>
    public static string Unpack(BackendLaunch launch, ExecutionOptions options)
    {
        var root = options.BackendsDirectory.Length > 0 ? options.BackendsDirectory : Path.Combine(ExtensionPackageCache.DefaultRoot, "backends");
        var folder = Path.Combine(root, $"{launch.Package.Id[..16]}-{launch.Address.Port}");
        Remove(folder);
        Directory.CreateDirectory(folder);
        launch.Package.UnpackBackend(folder);
        return folder;
    }

    /// <summary>
    /// Finds the entry point of a backend inside the folder it is unpacked to.
    /// </summary>
    /// <param name="launch">The <see cref="BackendLaunch"/>.</param>
    /// <param name="folder">The folder.</param>
    /// <returns>The full path of the entry point.</returns>
    /// <exception cref="BackendStartException">The entry point is not inside the folder or not there.</exception>
    public static string EntryPoint(BackendLaunch launch, string folder)
    {
        var entry = Path.GetFullPath(Path.Combine(folder, launch.Package.Contract.Backend!.Assembly));
        if (!entry.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new BackendStartException($"The entry point of {launch.System} is outside its package.");
        if (!File.Exists(entry)) throw new BackendStartException($"The package of {launch.System} has no {launch.Package.Contract.Backend.Assembly}.");
        return entry;
    }

    /// <summary>
    /// Removes an unpacked backend. A folder that cannot be removed is left for the next start to replace.
    /// </summary>
    /// <param name="folder">The folder.</param>
    public static void Remove(string folder)
    {
        try
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // Something still holds a file; the next start for this port unpacks over a fresh folder.
        }
        catch (UnauthorizedAccessException)
        {
            // As above.
        }
    }

    /// <summary>
    /// Gets the environment every backend is started with. It tells the backend where to listen, which is the one thing Stage cannot say over the protocol before it connects.
    /// </summary>
    /// <param name="launch">The <see cref="BackendLaunch"/>.</param>
    /// <param name="listen">The address to listen on, as <c lang="text">host:port</c>.</param>
    /// <returns>The variables.</returns>
    public static IReadOnlyDictionary<string, string> Environment(BackendLaunch launch, string listen) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [EnvironmentNames.Listen] = listen,
        [EnvironmentNames.System] = launch.System,
        [EnvironmentNames.PackageDigest] = launch.Package.Digest
    };
}

/// <summary>
/// The names of the environment variables a backend is started with.
/// </summary>
public static class EnvironmentNames
{
    /// <summary>
    /// The address to listen on, as <c lang="text">host:port</c>.
    /// </summary>
    public const string Listen = "STAGE_EXTENSION_LISTEN";

    /// <summary>
    /// The name of the system the backend serves.
    /// </summary>
    public const string System = "STAGE_EXTENSION_SYSTEM";

    /// <summary>
    /// The digest of the package the backend came from.
    /// </summary>
    public const string PackageDigest = "STAGE_EXTENSION_PACKAGE_DIGEST";
}
