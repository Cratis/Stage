// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Records one resolved profile input by identity and content digest. Prerelease: input provenance, outside the output-only digest.
/// </summary>
/// <param name="Name">The input name.</param>
/// <param name="Version">The input version.</param>
/// <param name="Sha256">The lowercase SHA-256 of the input bytes.</param>
public sealed record ArtifactRenderInputDigest(string Name, string Version, string Sha256);
