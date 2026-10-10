// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Finds the machine and the user that planned an artifact in its text.
/// </summary>
/// <remarks>
/// A user name is often an ordinary word - a CI runner's user is <c language="shell">runner</c>, which the generated
/// project carries in the package id <c language="shell">xunit.runner.visualstudio</c> - so it is matched only where it
/// identifies someone: as a path segment (<c language="shell">/home/runner/</c>, <c language="shell">C:\Users\runner\</c>)
/// or as the user of an identity (<c language="shell">runner@host</c>). The machine name and the local paths are distinctive
/// enough to be matched anywhere.
/// </remarks>
public static class MachineIdentity
{
    /// <summary>
    /// Gets the machine name, user profile, temporary, current and base paths of this process found in the text.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <returns>The values found.</returns>
    public static IEnumerable<string> FoundIn(string text) =>
        Paths().Where(value => text.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Concat(ContainsUser(text, Environment.UserName) ? [Environment.UserName] : []);

    /// <summary>
    /// Whether a text carries a user name as an identity: a path segment, or the user part of <c language="shell">user@host</c>.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="user">The user name.</param>
    /// <returns>True when the user name appears as an identity.</returns>
    public static bool ContainsUser(string text, string user)
    {
        if (user.Length == 0)
        {
            return false;
        }

        for (var index = text.IndexOf(user, StringComparison.OrdinalIgnoreCase); index >= 0; index = text.IndexOf(user, index + 1, StringComparison.OrdinalIgnoreCase))
        {
            var before = index > 0 ? text[index - 1] : '\0';
            var end = index + user.Length;
            var after = end < text.Length ? text[end] : '\0';
            var pathSegment = before is '/' or '\\' && !IsNameCharacter(after);
            var identity = after == '@' && !IsNameCharacter(before);
            if (pathSegment || identity)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsNameCharacter(char character) => char.IsLetterOrDigit(character) || character is '_' or '-' or '.';

    static IEnumerable<string> Paths() => new[]
    {
        Environment.MachineName,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
        Environment.CurrentDirectory,
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)
    }.Where(value => value.Length > 3);
}
