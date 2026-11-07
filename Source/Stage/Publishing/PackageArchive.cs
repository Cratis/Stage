// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;

namespace Cratis.Stage.Publishing;

/// <summary>
/// Writes a package as a deterministic <c language="shell">.tar.gz</c> and extracts one safely.
/// </summary>
internal static class PackageArchive
{
    const long MaximumExtractedBytes = 512L * 1024 * 1024;

    internal static byte[] Write(IReadOnlyDictionary<string, byte[]> files)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gzip, TarEntryFormat.Ustar, leaveOpen: true))
        {
            foreach (var file in files.OrderBy(file => file.Key, StringComparer.Ordinal))
            {
                var entry = new UstarTarEntry(TarEntryType.RegularFile, file.Key)
                {
                    DataStream = new MemoryStream(file.Value, writable: false),
                    ModificationTime = DateTimeOffset.UnixEpoch,
                    Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead,
                    Uid = 0,
                    Gid = 0,
                    UserName = string.Empty,
                    GroupName = string.Empty
                };
                tar.WriteEntry(entry);
            }
        }

        return output.ToArray();
    }

    /// <summary>
    /// Extracts the archive into a folder, refusing anything but plain files and folders that stay inside it.
    /// </summary>
    /// <param name="archivePath">The <c language="shell">.tar.gz</c> file.</param>
    /// <param name="target">The folder to extract into; it must exist.</param>
    /// <exception cref="InvalidDataException">The archive is damaged, holds a link or a special file, an entry that escapes the folder, or is too large.</exception>
    internal static void Extract(string archivePath, string target)
    {
        var root = Path.GetFullPath(target);
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        long total = 0;
        try
        {
            using var file = File.OpenRead(archivePath);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var tar = new TarReader(gzip);
            while (tar.GetNextEntry(copyData: false) is { } entry)
            {
                if (entry.EntryType is TarEntryType.ExtendedAttributes or TarEntryType.GlobalExtendedAttributes)
                {
                    continue;
                }

                var name = entry.Name.Replace('\\', '/');
                if (name.Length == 0 || name.StartsWith('/') || name.Contains(':', StringComparison.Ordinal) || name.Split('/').Any(segment => segment == ".."))
                {
                    throw new InvalidDataException($"The archive entry '{entry.Name}' is not a relative path inside the package.");
                }

                var destination = Path.GetFullPath(Path.Combine(root, name));
                if (!destination.StartsWith(prefix, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"The archive entry '{entry.Name}' would be written outside the package.");
                }

                switch (entry.EntryType)
                {
                    case TarEntryType.Directory:
                        Directory.CreateDirectory(destination);
                        break;
                    case TarEntryType.RegularFile or TarEntryType.V7RegularFile:
                        total += entry.Length;
                        if (total > MaximumExtractedBytes)
                        {
                            throw new InvalidDataException("The archive is larger than a package may be.");
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        entry.ExtractToFile(destination, overwrite: false);
                        break;
                    default:
                        throw new InvalidDataException($"The archive entry '{entry.Name}' is a {entry.EntryType}; only files and folders are accepted.");
                }
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException and not FileNotFoundException and not DirectoryNotFoundException || exception is FormatException)
        {
            throw new InvalidDataException($"The archive could not be read: {exception.Message}", exception);
        }
    }
}
