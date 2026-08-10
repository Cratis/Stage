// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Removes the <c>SomeModule/SomeFeature</c> sample slice the <c>Cratis.Templates</c> template ships with. A
/// rendered application brings its own slices, so the sample is dead weight that compiles into the app and shows
/// up as a real feature — it is removed together with the single route that composes it.
/// </summary>
public static class SampleSlice
{
    const string FolderName = "SomeModule";
    const string CompositionFileName = "App.tsx";

    /// <summary>
    /// Removes the sample slice folder and any composition line referencing it.
    /// </summary>
    /// <param name="targetDirectory">The scaffolded directory to remove the sample from.</param>
    /// <returns><see langword="true"/> if a sample slice was found and removed.</returns>
    public static bool Remove(DirectoryInfo targetDirectory)
    {
        var folder = new DirectoryInfo(Path.Combine(targetDirectory.FullName, FolderName));
        if (!folder.Exists)
        {
            return false;
        }

        folder.Delete(recursive: true);
        RemoveCompositionLines(targetDirectory);
        return true;
    }

    static void RemoveCompositionLines(DirectoryInfo targetDirectory)
    {
        var composition = Path.Combine(targetDirectory.FullName, CompositionFileName);
        if (!File.Exists(composition))
        {
            return;
        }

        var kept = File.ReadAllLines(composition).Where(line => !line.Contains(FolderName, StringComparison.Ordinal) && !line.Contains("SomeFeature", StringComparison.Ordinal));
        File.WriteAllLines(composition, kept);
    }
}
