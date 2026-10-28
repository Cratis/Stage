// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A fully validated, immutable kit. The engine never needs to revisit its source.</summary>
public sealed record LoadedKit(KitManifest Manifest, KitFiles Files, ImmutableArray<byte> Archive, string Digest);
