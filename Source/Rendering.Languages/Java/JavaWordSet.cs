// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Captures the conservative words of decoded Java text, without interpreting comments, literals or scopes.
/// </summary>
/// <param name="All">Every maximal ASCII word that does not start with a digit.</param>
/// <param name="Declaring">The words not followed by a dot after skipping Java whitespace.</param>
public sealed record JavaWordSet(ImmutableHashSet<string> All, ImmutableHashSet<string> Declaring);
