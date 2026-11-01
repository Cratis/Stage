// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Execution;

internal static class BodyStateNumbers
{
    internal static SemanticValue? Write(object value) => value switch
    {
        byte or sbyte or short or ushort or int or uint or long or ulong or decimal => SemanticValue.Number(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
        double or float => SemanticValue.Number(checked((decimal)Convert.ToDouble(value, CultureInfo.InvariantCulture))),
        _ => null
    };
}
