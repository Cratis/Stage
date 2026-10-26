// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal sealed class InlineCodeToken(string spelling)
{
    public override string ToString() => spelling;
}

internal sealed record InlineCodeTokenIssuance(InlineCodeToken? Token, ImmutableArray<InlineCodeDiagnostic> Diagnostics);
internal sealed record InlineCodeSlot(InlineCode Code, string Presentation);

internal sealed class InlineCodeTokens
{
    internal const string Prefix = "/*__stage_inline:";
    internal const string Suffix = "__*/";
    readonly Dictionary<string, InlineCodeSlot> _slots = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, InlineCodeSlot> Slots => _slots;

    internal InlineCodeTokenIssuance Issue(InlineCode code, InlineCodePlacement actualPlacement, int indent = 0, bool verbatim = false)
    {
        if (code.Placement != actualPlacement) return new(null, [new("STAGE-INLINE-INTEGRITY", "The verified placement differs from the insertion site.")]);
        var presentation = CSharpInlineLanguagePack.Present(code, indent, verbatim);
        if (presentation.Text is null) return new(null, presentation.Diagnostics);

        // Render-local capabilities: possession of an unpredictable token authenticates its immutable registry entry.
        // No unkeyed digest, semantic identity, or caller-supplied token can register a body.
        var spelling = Prefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)) + Suffix;
        _slots.Add(spelling, new(code, presentation.Text));
        return new(new(spelling), presentation.Diagnostics);
    }
}
