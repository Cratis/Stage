// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Describes an explicit semantic gap whose placeholder will be selected in a known grammar context.
/// </summary>
/// <param name="Code">The gap's existing diagnostic code, not a pack integrity refusal.</param>
/// <param name="Construct">The unsupported construct.</param>
/// <param name="Intent">The model's stated intent.</param>
/// <param name="Context">The grammar context: type, member, or statement in java@1.</param>
public sealed record GapSegment(string Code, string Construct, string Intent, string Context) : SymbolSegment
{
    internal override IEnumerable<string> Fields => [Code, Construct, Intent, Context];
}
