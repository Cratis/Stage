// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>The complete stage-target/v1.0 manifest. All arrays are required, including empty arrays.</summary>
public sealed record KitManifest(
    ContractVersion ApiVersion,
    KitTarget Target,
    ImmutableArray<LanguagePackVersion> LanguagePacks,
    ImmutableArray<ContractVersion> Requires,
    ImmutableArray<KitOutput> Outputs,
    ImmutableArray<KitPartial> Partials,
    ImmutableArray<KitTable> Tables,
    ImmutableArray<KitAsset> Assets,
    ImmutableArray<KitDiagnosticDeclaration> Diagnostics);
