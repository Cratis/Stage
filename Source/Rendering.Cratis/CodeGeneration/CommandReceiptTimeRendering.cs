// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Renders the receipt-time access shared by semantic and legacy command handlers.
/// </summary>
internal static class CommandReceiptTimeRendering
{
    internal static RenderedFile Render(string rootNamespace)
    {
        var builder = new CSharpCodeBuilder().Namespace($"{rootNamespace}.GeneratedCommands")
            .Summary("Reads the receipt time captured by the active Arc dispatch.")
            .OpenBlock("internal static class CommandReceiptTime")
            .Summary("Gets the dispatch receipt at Chronicle's UTC-millisecond precision.")
            .Line("/// <param name=\"operation\">The active Arc operation.</param>")
            .Line("/// <returns>The normalized dispatch receipt.</returns>")
            .Line("/// <exception cref=\"CommandReceiptTimeUnavailable\">No dispatch receipt is available.</exception>")
            .OpenBlock("internal static global::System.DateTimeOffset OccurredAtReceipt(global::Cratis.Arc.IOperationContextAccessor operation)")
            .Line("var receivedAt = operation.ReceivedAt ?? throw new CommandReceiptTimeUnavailable();")
            .OpenBlock("if (receivedAt == default)")
            .Line("throw new CommandReceiptTimeUnavailable();")
            .EndBlock()
            .Line("return global::System.DateTimeOffset.FromUnixTimeMilliseconds(receivedAt.ToUnixTimeMilliseconds());")
            .EndBlock()
            .EndBlock()
            .BlankLine()
            .Summary("The exception that is thrown when a command has no Arc receipt time.")
            .Line("internal sealed class CommandReceiptTimeUnavailable() : global::System.Exception(\"The command requires an active Arc operation with a receipt time.\");");
        return new(Path.Combine("GeneratedCommands", "CommandReceiptTime.cs"), builder.ToString());
    }
}
