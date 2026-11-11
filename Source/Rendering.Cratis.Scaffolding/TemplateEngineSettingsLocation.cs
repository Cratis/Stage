// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// A settings folder that belongs to one scaffold and is deleted when the scaffold ends.
/// </summary>
/// <remarks>
/// The template engine guards its global settings with a machine-wide named mutex derived from the settings path.
/// A shared path lets one process abandon the mutex another process waits on, which crashes the waiter, so every
/// scaffold gets its own settings location.
/// </remarks>
sealed class TemplateEngineSettingsLocation : IDisposable
{
    TemplateEngineSettingsLocation(string path) => Path = path;

    /// <summary>
    /// Gets the full path of the settings folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Creates a settings location unique to one scaffold, under the temporary directory.
    /// </summary>
    /// <returns>The new <see cref="TemplateEngineSettingsLocation"/>.</returns>
    public static TemplateEngineSettingsLocation Create() =>
        new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cratis-stage-template-engine", Guid.NewGuid().ToString("N")));

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover settings folder in the temporary directory is harmless.
        }
        catch (UnauthorizedAccessException)
        {
            // A leftover settings folder in the temporary directory is harmless.
        }
    }
}
