// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Publishing;

/// <summary>
/// Computes the digest of the files of a model, independent of file system order and timestamps.
/// </summary>
internal static class PackageDigest
{
    internal static string Of(string folder) => Of(Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
        .Select(file => new KeyValuePair<string, byte[]>(Path.GetRelativePath(folder, file), File.ReadAllBytes(file))));

    internal static string Of(IEnumerable<KeyValuePair<string, byte[]>> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files.OrderBy(file => file.Key.Replace('\\', '/'), StringComparer.Ordinal))
        {
            builder.Append(file.Key.Replace('\\', '/')).Append('\n')
                .Append(Convert.ToHexString(SHA256.HashData(file.Value)).ToLowerInvariant()).Append('\n');
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant()}";
    }
}
