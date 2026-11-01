// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>A content-addressed PE store. Every read verifies the bytes before use.</summary>
internal static class BodyUnitCache
{
    internal static string PePath(string cacheDirectory, string hash) => Path.Combine(cacheDirectory, $"{hash}.dll");

    internal static BodyUnitEntry? Read(string cacheDirectory, string hash)
    {
        if (hash.Length != 64 || hash.Any(character => !char.IsAsciiHexDigit(character))) return null;
        var manifestPath = Path.Combine(cacheDirectory, $"{hash}.json");
        if (!File.Exists(manifestPath)) return null;
        try
        {
            var entry = JsonSerializer.Deserialize<BodyUnitEntry>(File.ReadAllBytes(manifestPath), BodyWorkerMessages.Json);
            return entry?.UnitHash == hash && Verify(cacheDirectory, entry) ? entry : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            return null;
        }
    }

    internal static bool Verify(string cacheDirectory, BodyUnitEntry entry)
    {
        if (entry.UnitHash is not { Length: 64 } hash || hash.Any(character => !char.IsAsciiHexDigit(character))) return false;
        return ReadPe(cacheDirectory, entry) is not null;
    }

    internal static byte[]? ReadPe(string cacheDirectory, BodyUnitEntry entry)
    {
        var path = PePath(cacheDirectory, entry.UnitHash!);
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        return Convert.ToHexString(SHA256.HashData(bytes)) == entry.PeSha256 ? bytes : null;
    }

    internal static void Write(string cacheDirectory, BodyUnitEntry entry, byte[] pe)
    {
        Directory.CreateDirectory(cacheDirectory);
        AtomicWrite(PePath(cacheDirectory, entry.UnitHash!), pe);
        AtomicWrite(Path.Combine(cacheDirectory, $"{entry.UnitHash}.json"), JsonSerializer.SerializeToUtf8Bytes(entry, BodyWorkerMessages.Json));
    }

    static void AtomicWrite(string path, byte[] bytes)
    {
        var temporary = $"{path}.{Guid.NewGuid():N}.pending";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
