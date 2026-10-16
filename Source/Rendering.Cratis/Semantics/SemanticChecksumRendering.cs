// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticChecksumRendering
{
    internal static string? Algorithm(SemanticValue? operand) => operand is SemanticTextValue text ? text.Value switch
    {
        "luhn" => "Luhn",
        "mod11" => "Mod11",
        "mod97" => "Mod97",
        _ => null
    } : null;
}
