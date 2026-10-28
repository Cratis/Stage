// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>A body-owned declaration and the scopes in which its spelling must be unique.</summary>
/// <param name="Key">The role-qualified semantic identity.</param>
/// <param name="Stem">The readable declaration name before normalization.</param>
/// <param name="Identity">The stable implementation requirement identity.</param>
/// <param name="Scope">The declaring member or namespace scope.</param>
/// <param name="TypeScope">An optional scope of types visible to the authored body.</param>
/// <param name="PathScope">An optional case-insensitive output directory scope.</param>
internal sealed record SemanticInlineNameSite(string Key, string Stem, string Identity, string Scope, string? TypeScope = null, string? PathScope = null);

/// <summary>An existing modeled or generated declaration that a helper must not displace.</summary>
/// <param name="Scope">The scope containing the declaration.</param>
/// <param name="Name">Its final, already normalized spelling.</param>
internal sealed record SemanticInlineNameReservation(string Scope, string Name);
