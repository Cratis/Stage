// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Provides the internal source of truth for audited Cratis ESM support and its prospective range.
/// </summary>
internal static class CratisEsmSupport
{
    internal static EsmSupport Descriptor { get; } = new(
        [.. EsmSchemaV30Support.LanguageVersions.Zip(EsmSchemaV30Support.SemanticVersions), (LanguageVersion.V32, SemanticVersion.V32), (LanguageVersion.V33, SemanticVersion.V33), (LanguageVersion.V34, SemanticVersion.V34)],
        new(SemanticVersion.V32, 33));
}
