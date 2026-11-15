// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The <c language="shell">.play</c> sources of a compilation, held as text and served to Screenplay's folder compiler as if they sat in one folder.
/// </summary>
/// <param name="name">The folder name the compiler is asked to compile; the sources sit directly in it.</param>
/// <param name="texts">The source text of each document, by its portable path.</param>
internal sealed class SourceTextFiles(string name, IReadOnlyDictionary<string, string> texts) : IPlayFiles
{
    /// <inheritdoc/>
    public IEnumerable<PlayFile> FindIn(string root)
    {
        var relative = root.Replace('\\', '/')[name.Length..].TrimStart('/');
        var prefix = relative.Length == 0 ? string.Empty : $"{relative}/";

        return texts.Keys.Where(path => path.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)
            .Select(path => new PlayFile(path, path[prefix.Length..]));
    }

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => texts.TryGetValue(file.Path, out var text)
        ? text
        : throw new InvalidOperationException($"The source '{file.Path}' is not one of the sources of this compilation.");
}
