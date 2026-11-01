// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>The canonical kit archiver: ordinal regular-file ustar, fixed metadata, no compression.</summary>
public static class KitArchive
{
    const int BlockSize = 512;

    /// <summary>Writes immutable files with fixed mode 0644, zero owners/times and two zero end blocks.</summary>
    /// <param name="files">Portable kit-relative files, including stage-kit.json.</param>
    /// <returns>The deterministic archive bytes.</returns>
    public static ImmutableArray<byte> Write(KitFiles files)
    {
        using var stream = new MemoryStream();
        foreach (var path in files.Paths)
        {
            if (KitPath.Validate(path) is not null)
            {
                throw new ArgumentException("Archive paths must be portable kit-relative paths.", nameof(files));
            }

            var bytes = files[path];
            stream.Write(Header(path, bytes.Length));
            stream.Write(bytes.AsSpan());
            var padding = (BlockSize - (bytes.Length % BlockSize)) % BlockSize;
            stream.Write(new byte[padding]);
        }

        stream.Write(new byte[BlockSize * 2]);
        return [.. stream.ToArray()];
    }

    /// <summary>Exposes a tar archive as a source; the loader rejects links and non-regular entries.</summary>
    /// <param name="archive">Uncompressed archive bytes.</param>
    /// <returns>A lazily read source subject to normal loader checks.</returns>
    public static IKitSource Read(ImmutableArray<byte> archive) => new ArchiveKitSource(archive);

    static byte[] Header(string path, int length)
    {
        var header = new byte[BlockSize];
        var split = path.Length <= 100 ? -1 : path.LastIndexOf('/', Math.Min(path.Length - 1, 155));
        Text(header, 0, split < 0 ? path : path[(split + 1)..]);
        if (split >= 0)
        {
            Text(header, 345, path[..split]);
        }

        Octal(header, 100, 8, 420);
        Octal(header, 108, 8, 0);
        Octal(header, 116, 8, 0);
        Octal(header, 124, 12, length);
        Octal(header, 136, 12, 0);
        header.AsSpan(148, 8).Fill(32);
        header[156] = (byte)'0';
        Text(header, 257, "ustar");
        Text(header, 263, "00");
        var checksum = header.Sum(value => value);
        Octal(header, 148, 7, checksum);
        header[155] = 32;
        return header;
    }

    static void Text(byte[] header, int offset, string value) => Encoding.ASCII.GetBytes(value, header.AsSpan(offset));

    static void Octal(byte[] header, int offset, int width, long value) => Text(header, offset, Convert.ToString(value, 8).PadLeft(width - 1, '0'));
}
