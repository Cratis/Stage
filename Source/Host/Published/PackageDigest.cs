// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Computes the digest of the files of a model folder, independent of file system order and timestamps.
/// </summary>
internal static class PackageDigest
{
    internal static string Of(string folder)
    {
        var builder = new StringBuilder();
        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal))
        {
            builder.Append(file).Append('\n')
                .Append(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder, file)))).ToLowerInvariant()).Append('\n');
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant()}";
    }
}
