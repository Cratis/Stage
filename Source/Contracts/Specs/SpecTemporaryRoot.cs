// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specs;

/// <summary>
/// Hands specs a per-spec temporary path inside one root the specs own, so that a run which is aborted before
/// its teardown leaves its leftovers in a single known folder that the next run clears. The root lives beneath
/// the user's own application data folder, which other users cannot write to; where that is unavailable the run
/// gets a private temporary folder of its own instead, which is not reused and so never swept.
/// </summary>
public static class SpecTemporaryRoot
{
    static readonly TimeSpan _staleAfter = TimeSpan.FromDays(1);
    static readonly Lazy<string> _root = new(() => PrepareRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));

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
    /// a link is left entirely alone.
    /// </summary>
    /// <param name="root">The folder to sweep.</param>
    /// <param name="olderThanUtc">Entries last written before this moment are removed.</param>
    internal static void Sweep(string root, DateTime olderThanUtc)
    {
        var directory = new DirectoryInfo(root);
        if (!directory.Exists || directory.LinkTarget is not null)
        {
            return;
        }

        // Leftovers from a run that was aborted before teardown. Anything recent may belong to a run that is
        // still going, and a failure to remove one never fails the spec that happened to be first.
        try
        {
            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (entry.LastWriteTimeUtc >= olderThanUtc)
                {
                    continue;
                }

                try
                {
                    if (entry is DirectoryInfo child)
                    {
                        child.Delete(recursive: true);
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

    /// <summary>
    /// Prepares the sweepable root beneath a user-private base folder. When the base is empty, or the root cannot
    /// be created or used there, a fresh temporary folder for this process is returned and nothing is swept.
    /// </summary>
    /// <param name="baseDirectory">A folder only the current user can write to.</param>
    /// <returns>The folder to hand paths out in.</returns>
    internal static string PrepareRoot(string baseDirectory)
    {
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            try
            {
                var root = Path.Combine(baseDirectory, "cratis", "stage-spec-roots");
                Directory.CreateDirectory(root);
                Sweep(root, DateTime.UtcNow - _staleAfter);
                return root;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The base is unusable (read-only, not a folder, a dangling link at the root name): use the
                // process-private folder below instead of failing every spec that asks for a path.
            }
        }

        return Directory.CreateTempSubdirectory("stage-spec-").FullName;
    }
}
