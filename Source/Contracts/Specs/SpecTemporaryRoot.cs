// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specs;

/// <summary>
/// Hands specs a per-spec temporary path inside one root the specs own, so that a run which is aborted before
/// its teardown leaves its leftovers in a single known folder that the next run clears.
/// </summary>
public static class SpecTemporaryRoot
{
    const string RootName = "stage-spec-roots";
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

    static string PrepareRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), RootName);
        Directory.CreateDirectory(root);

        // Leftovers from a run that was aborted before teardown. Anything recent may belong to a run that is
        // still going, so only entries that have gone untouched for a day are removed, and a failure to remove
        // one never fails the spec that happened to be first.
        foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
        {
            if (DateTime.UtcNow - entry.LastWriteTimeUtc < _staleAfter)
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

        return root;
    }
}
