// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specs;

/// <summary>
/// Hands specs a per-spec temporary path inside one root the specs own, so that a run which is aborted before
/// its teardown leaves its leftovers in a single known folder that the next run clears.
/// </summary>
public static class SpecTemporaryRoot
{
    const UnixFileMode PrivateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    static readonly string _rootName = $"stage-spec-roots-{UserSegment()}";
    static readonly TimeSpan _staleAfter = TimeSpan.FromDays(1);
    static readonly Lazy<string> _root = new(PrepareRoot);

    /// <summary>
    /// Gets a new, unique, not yet created path beneath the spec-owned root.
    /// </summary>
    /// <param name="prefix">A short name for what the path is for; it is the start of the folder name.</param>
    /// <returns>The full path. The caller creates it when needed and deletes it in teardown with <see cref="Delete"/>.</returns>
    public static string NewPath(string prefix) => Path.Combine(_root.Value, $"{prefix}-{Guid.NewGuid():N}");

    /// <summary>
    /// Deletes a path handed out by <see cref="NewPath"/>, along with everything beneath it, when it exists.
    /// </summary>
    /// <param name="path">The path to delete.</param>
    public static void Delete(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>
    /// Removes the entries directly beneath a root that have not been written since the given time. A root that is
    /// a link, or on Unix is not private to the current user, is left entirely alone.
    /// </summary>
    /// <param name="root">The folder to sweep.</param>
    /// <param name="olderThanUtc">Entries last written before this moment are removed.</param>
    internal static void Sweep(string root, DateTime olderThanUtc)
    {
        if (!IsSafe(root))
        {
            return;
        }

        // Leftovers from a run that was aborted before teardown. Anything recent may belong to a run that is
        // still going, and a failure to remove one never fails the spec that happened to be first.
        try
        {
            foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
            {
                if (entry.LastWriteTimeUtc >= olderThanUtc)
                {
                    continue;
                }

                try
                {
                    if (entry is DirectoryInfo directory)
                    {
                        directory.Delete(recursive: true);
                    }
                    else
                    {
                        entry.Delete();
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Best-effort sweep of an earlier run's leftovers: an entry still in use is left for the next run.
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The root could not be listed; there is nothing to sweep.
        }
    }

    static string UserSegment()
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string([.. Environment.UserName.Select(character => Array.IndexOf(invalid, character) >= 0 ? '_' : character)]);
    }

    static string PrepareRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), _rootName);
        CreateRoot(root);

        // The temporary folder is shared on some machines, so another user may have planted something at this
        // name. A root that is not provably this user's private folder is neither swept nor written into; the
        // run gets a fresh private folder instead, which is simply not reused by a later run.
        if (!IsSafe(root))
        {
            return Directory.CreateTempSubdirectory($"{_rootName}-").FullName;
        }

        Sweep(root, DateTime.UtcNow - _staleAfter);
        return root;
    }

    static void CreateRoot(string root)
    {
        // An existing root, a link included, is left as it is; IsSafe decides whether it can be used.
        if (Directory.Exists(root))
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(root);
        }
        else
        {
            Directory.CreateDirectory(root, PrivateMode);
        }
    }

    static bool IsSafe(string root)
    {
        var directory = new DirectoryInfo(root);
        if (!directory.Exists || directory.LinkTarget is not null)
        {
            return false;
        }

        // The mode is the only ownership signal .NET exposes. Together with the per-user name, a root that is
        // exactly user-only (0700) is one nobody else can have written into, and nobody else can swap for a link
        // inside, whereas a group- or other-writable one may belong to, or be altered by, someone else.
        return OperatingSystem.IsWindows() || directory.UnixFileMode == PrivateMode;
    }
}
