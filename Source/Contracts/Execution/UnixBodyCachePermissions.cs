// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace Cratis.Stage.Contracts.Execution;

[UnsupportedOSPlatform("windows")]
internal static class UnixBodyCachePermissions
{
    internal static void Validate(string path) => ValidateHierarchy(path, cache: true);

    static void ValidateHierarchy(string path, bool cache)
    {
        for (DirectoryInfo? directory = new(path); directory is not null; directory = directory.Parent)
        {
            Validate(directory, cache);
            cache = false;
        }
    }

    static void Validate(DirectoryInfo directory, bool cache)
    {
        var owner = Owner(directory.FullName);
        var currentUser = owner == Environment.UserName;
        if (directory.LinkTarget is not null)
        {
            if (!currentUser && owner != "root") throw new UnauthorizedAccessException("Body cache parent symbolic link has an untrusted owner.");
            ValidateHierarchy(directory.ResolveLinkTarget(true)!.FullName, cache: false);
            return;
        }
        var mode = directory.UnixFileMode;
        var writable = (mode & (UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0;
        var sharedTemporaryParent = !cache && owner == "root" && (mode & UnixFileMode.StickyBit) != 0;
        if ((!currentUser && (cache || owner != "root")) || (writable && !sharedTemporaryParent))
        {
            throw new UnauthorizedAccessException("Body cache must be owned by the current user and protected from other users, including its parent directories.");
        }
    }

    static string Owner(string path)
    {
        using var process = Process.Start(OwnershipCommand(path)) ?? throw new IOException("Cannot inspect body cache ownership.");
        if (!process.WaitForExit(5000))
        {
            process.Kill(entireProcessTree: true);
            throw new IOException("Cannot inspect body cache ownership.");
        }
        if (process.ExitCode != 0) throw new IOException("Cannot inspect body cache ownership.");
        return process.StandardOutput.ReadToEnd().Trim();
    }

    static ProcessStartInfo OwnershipCommand(string path)
    {
        // Use the OS's trusted metadata utility rather than guessing platform-specific struct stat layouts.
        var executable = File.Exists("/usr/bin/stat") ? "/usr/bin/stat" : "/bin/stat";
        var info = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (OperatingSystem.IsMacOS())
        {
            info.ArgumentList.Add("-f");
            info.ArgumentList.Add("%Su");
        }
        else
        {
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add("%U");
            info.ArgumentList.Add("--");
        }
        info.ArgumentList.Add(path);
        return info;
    }
}
