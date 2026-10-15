// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticChecksumRules
{
    internal static Checksum? Algorithm(SemanticValue? operand) => operand is SemanticTextValue text ? text.Value switch
    {
        "luhn" => Checksum.Luhn,
        "mod11" => Checksum.Mod11,
        "mod97" => Checksum.Mod97,
        _ => null
    } : null;

    internal static bool Satisfies(SemanticValue value, SemanticValue? operand, Func<Exception> unsupported) =>
        value is SemanticTextValue text && Algorithm(operand) is { } algorithm
            ? GenerateValue.HasValidChecksum(text.Value, algorithm)
            : throw unsupported();
}
