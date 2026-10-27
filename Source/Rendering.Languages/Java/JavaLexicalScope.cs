// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Describes an emitted lexical interval. Opaque/anonymous scopes never supply a generated owner identity.
/// </summary>
/// <param name="Kind">The recognized type, block, initializer, anonymous, or unknown scope kind.</param>
/// <param name="Start">The raw start, including named-type headers and type parameters.</param>
/// <param name="Body">The raw body-opening brace position.</param>
/// <param name="End">The exclusive raw end.</param>
/// <param name="Name">The actual emitted declaration name, if named.</param>
/// <param name="Declaration">The structured declaration segment identity, or -1.</param>
/// <param name="BaseType">The erased anonymous base type, when classified.</param>
public sealed record JavaLexicalScope(string Kind, int Start, int Body, int End, string Name, int Declaration = -1, TypeName? BaseType = null)
{
    /// <summary>Gets whether this interval is a named or anonymous type scope.</summary>
    public bool IsType => Kind is "class" or "interface" or "enum" or "record" or "opaque" or "anonymous" or "enum-constant";

    /// <summary>Gets whether the interval may introduce unmodeled inherited bindings.</summary>
    public bool IsOpaque => Kind is "opaque" or "anonymous" or "enum-constant" or "unknown";
}
