// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Holds an authored native span. Validation scans separately; it never rewrites the body or its line endings.
/// </summary>
/// <param name="Language">The native language identity.</param>
/// <param name="Body">The authored span with original bytes represented as text.</param>
/// <param name="Owner">The stable model owner identity.</param>
/// <param name="Context">The grammar context: type, member, or statement in java@1.</param>
/// <param name="Scope">Types available to this native span.</param>
public sealed record NativeSegment(string Language, string Body, string Owner, string Context, IReadOnlyList<TypeName> Scope) : SymbolSegment
{
    internal override IEnumerable<string> Fields => new[] { Language, Body, Owner, Context }.Concat(Scope.SelectMany(type => type.Fields));
}
