// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Reads the files of a package from tar archives, gzipped or not, without trusting anything in them: a path that leaves the package, a link, a device, a duplicate,
/// or more bytes or files than the limits allow refuses the whole package. Nothing is written to disk and nothing is run.
/// </summary>
/// <param name="maxBytes">The most bytes the package may unpack to.</param>
/// <param name="maxFiles">The most files the package may hold.</param>
public sealed class PackageArchiveReader(long maxBytes, int maxFiles)
{
    /// <summary>
    /// Reads the files under a folder of the archives.
    /// </summary>
    /// <param name="archives">The archives, in order. A path may appear in only one.</param>
    /// <param name="root">The folder in the archives the package lives in, such as <c lang="text">extension</c> or <c lang="text">package</c>.</param>
    /// <returns>The <see cref="PackageContents"/>.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the package is not safe to read.</exception>
    public PackageContents Read(IEnumerable<Stream> archives, string root)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var backend = new List<PackageFileInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;

        foreach (var archive in archives)
        {
            using var stream = Decompressed(archive);
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry(copyData: false) is { } entry)
            {
                if (entry.EntryType is TarEntryType.Directory or TarEntryType.GlobalExtendedAttributes) continue;
                if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)) Refuse($"'{entry.Name}' is a {entry.EntryType}; only files and folders are allowed.");

                var path = Relative(entry.Name, root);
                if (!seen.Add(path)) Refuse($"'{path}' appears more than once.");
                if (seen.Count > maxFiles) Refuse($"The package holds more than {maxFiles} files.", PackageRefusalReason.TooLarge);

                var data = entry.DataStream is null ? [] : ReadBounded(entry.DataStream, maxBytes - total);
                total += data.Length;
                if (path.StartsWith(PackageContents.BackendFolder, StringComparison.Ordinal)) backend.Add(new(path, data.Length));
                else files[path] = data;
            }
        }

        return new(files, backend);
    }

    static Stream Decompressed(Stream archive)
    {
        var magic = new byte[2];
        var read = archive.Read(magic, 0, 2);
        archive.Seek(0, SeekOrigin.Begin);
        return read == 2 && magic[0] == 0x1f && magic[1] == 0x8b ? new GZipStream(archive, CompressionMode.Decompress, leaveOpen: true) : archive;
    }

    static byte[] ReadBounded(Stream stream, long remaining)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > remaining) Refuse("The package unpacks to more than the deployment allows.", PackageRefusalReason.TooLarge);
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    static string Relative(string name, string root)
    {
        var path = name.StartsWith("./", StringComparison.Ordinal) ? name[2..] : name;
        if (path.Length == 0 || path.StartsWith('/') || path.Contains('\\') || path.Contains('\0') || path.Split('/').Any(part => part is ".." or "." or ""))
        {
            Refuse($"'{name}' is not a safe path.");
        }

        var prefix = root.Trim('/') + "/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || path.Length == prefix.Length) Refuse($"'{name}' is not under '{prefix}'.");

        return path[prefix.Length..];
    }

    static void Refuse(string details, PackageRefusalReason reason = PackageRefusalReason.Malformed) => throw new PackageRefusedException(reason, details);
}
