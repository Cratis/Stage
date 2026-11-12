// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// The Notifier extension of <c language="csharp">Samples/Extensions/Notifier</c>, as the files of a package, and the model that uses it.
/// </summary>
public static class NotifierSample
{
    /// <summary>
    /// The reference the lock pins the sample under, which a system names in its <c language="csharp">extension</c> line.
    /// </summary>
    public const string Reference = "ghcr.io/acme/extension-mail";

    /// <summary>
    /// A model with a system the Notifier realizes and a screen that places its component.
    /// </summary>
    public const string Model = """
        concept NoticeId : Uuid
        concept NoticeText : String

        system Notifier
          description "Posts notices to a chat channel"
          extension "ghcr.io/acme/extension-mail"

        module Notices
          feature Board
            slice StateChange PostNotice
              command PostNotice
                noticeId NoticeId identifier
                text NoticeText
                produces NoticePosted
                  for noticeId
                  noticeId = noticeId
                  text = text
              event NoticePosted
                noticeId NoticeId
                text NoticeText
            slice StateChange DismissNotice
              command DismissNotice
                noticeId NoticeId identifier
                produces NoticeDismissed
                  for noticeId
                  noticeId = noticeId
              event NoticeDismissed
                noticeId NoticeId
            slice StateView NoticeBoard
              readmodel NoticeBoardItem
                noticeId NoticeId
                text NoticeText
              query GetNotice => NoticeBoardItem optional
                by noticeId NoticeId
              projection NoticeBoardItems => NoticeBoardItem
                from NoticePosted key noticeId
                  text = text
              screen NoticeBoard
                title "Notices"
                component Notifier.NoticeBoard board
                  property title = "Today"
                  on dismissed
                    execute DismissNotice
                      with noticeId from signal.noticeId
        """;

    /// <summary>
    /// Reads the sample from the repository, since it is the thing a person would copy.
    /// </summary>
    /// <returns>The files of the package, by path.</returns>
    /// <exception cref="InvalidOperationException">When the repository root is not found.</exception>
    public static Dictionary<string, byte[]> Files()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "Stage.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository root was not found.");
        }

        var sample = Path.Combine(root, "Samples", "Extensions", "Notifier");
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(sample, "*", SearchOption.AllDirectories).Where(path => !path.Contains($"{Path.DirectorySeparatorChar}backend{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            files[Path.GetRelativePath(sample, file).Replace('\\', '/')] = File.ReadAllBytes(file);
        }

        // The backend is built from backend/, so the package carries its assembly under the name the contract gives.
        files["backend/Notifier.dll"] = [1, 2, 3];
        return files;
    }
}
