// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler.given;

internal static class an_enum_validation_model
{
    internal const string Enumeration = "concept Status : Enum\n  open = 7\n  closed = -3\n";

    internal static string Command(string body, bool iterator = false) => Enumeration +
        (iterator ? validation_body_model.IteratorCommand(body) : validation_body_model.Command(body))
            .Replace("OrderId generated identifier", "OrderId identifier", StringComparison.Ordinal)
            .Replace("amount Decimal", "status Status", StringComparison.Ordinal)
            .Replace("amount rule", "status rule", StringComparison.Ordinal)
            .Replace("amount = amount", "status = status", StringComparison.Ordinal);
}
#endif
