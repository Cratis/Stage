// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Creates and validates the private directory containing executable body units.</summary>
public static class BodyWorkerCacheDirectory
{
    /// <summary>Gets the per-user cache location, never a shared temporary directory.</summary>
    public static string Default => DefaultPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    /// <summary>Normalizes the path and refuses an untrusted cache or replaceable parent directory.</summary>
    /// <param name="path">The configured cache directory.</param>
    /// <returns>The absolute, validated cache directory.</returns>
    /// <exception cref="UnauthorizedAccessException">The directory is not private to the current user.</exception>
    public static string Prepare(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (new DirectoryInfo(fullPath).LinkTarget is not null) throw new UnauthorizedAccessException("Body cache must not be a symbolic link.");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(fullPath);
            WindowsBodyCachePermissions.Validate(fullPath);
        }
        else
        {
            Directory.CreateDirectory(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            UnixBodyCachePermissions.Validate(fullPath);
        }
        return fullPath;
    }

    static string DefaultPath(string localData) => Path.IsPathFullyQualified(localData)
        ? Path.Combine(localData, "Cratis", "Stage", "Bodies")
        : Path.Combine(Path.GetTempPath(), $"cratis-stage-bodies-{Environment.UserName}");
}
