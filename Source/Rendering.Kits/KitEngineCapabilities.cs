// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Engine-owned versions available to a kit; no target-specific dependencies are implied.</summary>
public sealed record KitEngineCapabilities(
    int TargetApiMinor,
    ImmutableArray<LanguagePackVersion> LanguagePacks,
    ImmutableArray<ContractVersion> Contracts);
