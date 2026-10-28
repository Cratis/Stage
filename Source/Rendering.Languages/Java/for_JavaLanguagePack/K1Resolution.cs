// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Rendering.Declarative;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

/// <summary>Helpers shared by the K1 corpus and golden specs.</summary>
public static class K1Resolution
{
    public static string Text(DeclarativeFileResolution resolution) => string.Concat(resolution.Pieces.Select(piece => piece switch
    {
        DeclarativeGeneratedPiece generated => generated.Text,
        DeclarativeAuthoredPiece authored => authored.Text,
        _ => string.Empty
    }));

    public static string Digest(string text) => Convert.ToHexStringLower(SHA256.HashData(new UTF8Encoding(false).GetBytes(text)));

    public static IReadOnlyDictionary<string, DeclarativeFileResolution> ByPath(ImmutableArray<DeclarativeLanguageFile> files, ImmutableArray<DeclarativeFileResolution> resolutions) =>
        files.Select((file, index) => (file.Path, Resolution: resolutions[index])).ToDictionary(item => item.Path, item => item.Resolution, StringComparer.Ordinal);
}
