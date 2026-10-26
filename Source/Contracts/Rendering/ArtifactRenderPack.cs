// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>An effective exact package pin supplied by its provider, not a default guessed by Contracts.</summary>
/// <param name="Name">The package identity.</param>
/// <param name="Version">The provider's effective pinned version.</param>
public sealed record ArtifactRenderPack(string Name, string Version);
