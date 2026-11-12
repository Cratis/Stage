// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages.given;

/// <summary>
/// Builds extension packages, signing keys and lock entries for specifications.
/// </summary>
public static class PackageFixtures
{
    public const string Reference = "acme/mail";
    public const string Resolved = "ghcr.io/acme/extension-mail";
    public const string Publisher = "https://github.com/acme";
    public const string StageVersion = "5.1.0";
    public const string ScreenplayVersion = "6.22.0";
    public const string Placeholder = "<html><body><style>p{color:red}</style><p>Mail preview</p></body></html>";

    public static string Contract(string stage = "^5", string placeholder = "placeholders/preview.html", string extra = "", string display = "Transactional mail") => $$"""
        extension Mail
          display name "{{display}}"
          description "Sends mail"
          icon "assets/icon.svg"
          image "assets/screenshot.png"
            caption "The inbox"
          requires stage "{{stage}}"

          setting apiKey secret

          component MailPreview
            kind react
            platform web
            placeholder "{{placeholder}}"
            property subject String

          frontend "frontend/index.js"
        {{extra}}
        """.Replace("\r\n", "\n") + "\n";

    public static Dictionary<string, byte[]> Files(string? contract = null, string? placeholder = null) => new(StringComparer.Ordinal)
    {
        ["contract.play"] = Encoding.UTF8.GetBytes(contract ?? Contract()),
        ["assets/icon.svg"] = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"),
        ["assets/screenshot.png"] = [1, 2, 3, 4],
        ["placeholders/preview.html"] = Encoding.UTF8.GetBytes(placeholder ?? Placeholder),
        ["frontend/index.js"] = Encoding.UTF8.GetBytes("export default {};"),
        ["frontend/style.css"] = Encoding.UTF8.GetBytes("body{}"),
        ["README.md"] = Encoding.UTF8.GetBytes("not served")
    };

    public static byte[] Archive(string root, IEnumerable<KeyValuePair<string, byte[]>> files, bool gzip = true)
    {
        using var output = new MemoryStream();
        using (var stream = gzip ? new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true) : (Stream)output)
        using (var writer = new TarWriter(stream, TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var (path, bytes) in files)
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, $"{root}/{path}") { DataStream = new MemoryStream(bytes) });
            }
        }

        return output.ToArray();
    }

    public static byte[] RawArchive(params (string Name, TarEntryType Type, byte[] Data)[] entries)
    {
        using var output = new MemoryStream();
        using (var writer = new TarWriter(output, TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var (name, type, data) in entries)
            {
                var entry = new PaxTarEntry(type, name);
                if (type is TarEntryType.SymbolicLink or TarEntryType.HardLink) entry.LinkName = "/etc/passwd";
                else entry.DataStream = new MemoryStream(data);
                writer.WriteEntry(entry);
            }
        }

        return output.ToArray();
    }

    public static ECDsa NewKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public static string Pem(ECDsa key) => key.ExportSubjectPublicKeyInfoPem();

    public static byte[] CosignPayload(string digest) => Encoding.UTF8.GetBytes($$"""{"critical":{"identity":{"docker-reference":"{{Resolved}}"},"image":{"docker-manifest-digest":"{{digest}}"},"type":"cosign container image signature"},"optional":null}""");

    public static PackageSignature Sign(ECDsa key, byte[] payload) => new(payload, key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));

    public static ExtensionLockEntry Entry(byte[] archive, byte[] contract, string? digest = null, string publisher = Publisher, string resolved = Resolved) =>
        new(Reference, "latest", resolved, "1.2.0", digest ?? ExtensionHashes.Of(archive), ExtensionHashes.Of(contract), publisher);
}
