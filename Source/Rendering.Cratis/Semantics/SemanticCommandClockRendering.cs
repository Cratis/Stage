// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders elapsed UTC clock operands from the existing Arc operation receipt.
/// </summary>
internal static class SemanticCommandClockRendering
{
    // ValidationContext belongs to one validation invocation; never cache a receipt in a reusable validator instance.
    internal const string ValidationInstant = "((global::System.DateTimeOffset)validation.RootContextData[\"Cratis.Stage.CommandReceipt\"])";

    internal static void RenderValidationReceipt(CSharpCodeBuilder builder, SemanticApplicationContext application) =>
        builder.Line($"RuleFor(_ => _).Custom((_, validation) => validation.RootContextData[\"Cratis.Stage.CommandReceipt\"] = {Instant(application, "operation")});");

    internal static string Instant(SemanticApplicationContext application, string operation) =>
        $"global::{application.RootNamespace}.GeneratedCommands.CommandReceiptTime.InstantAtReceipt({operation})";

    internal static string Operand(SemanticClockOperand clock, string instant)
    {
        if (clock.Kind != SemanticClockKind.Now || clock.DateOf) throw UnsupportedSemanticRendering.For(nameof(SemanticClockOperand), clock.Kind);
        var amount = clock.Amount.ToString(CultureInfo.InvariantCulture);
        return clock.Unit switch
        {
            SemanticClockUnit.Day => $"({instant}).AddHours(24d * {amount})",
            SemanticClockUnit.Hour => $"({instant}).AddHours({amount})",
            SemanticClockUnit.Minute => $"({instant}).AddMinutes({amount})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticClockUnit), clock.Unit)
        };
    }
}
