// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a legacy inline handler is not proven context-independent.
/// </summary>
/// <param name="commandName">The authored command name.</param>
/// <param name="fullSlicePath">The full selected slice path.</param>
/// <param name="location">The exact source location of the authored code block.</param>
/// <param name="reason">The reason admission failed.</param>
/// <param name="innerException">An optional symbol-analysis failure.</param>
public class UnsupportedInlineCommandHandler(
    string commandName,
    string fullSlicePath,
    SourceLocation location,
    InlineCommandHandlerRejectionReason reason,
    Exception? innerException = null)
    : Exception(
        $"{DiagnosticCode}: Command '{commandName}' in slice '{fullSlicePath}' at {location.Path}({location.Line},{location.Column}) " +
        $"has an unsupported inline handler: {reason}. Only C# bodies proven independent of the generated context parameter can be rendered.",
        innerException)
{
    /// <summary>
    /// The stable diagnostic code for legacy inline handler rejection.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-INLINE-001";

    /// <summary>
    /// Gets the authored command name.
    /// </summary>
    public string CommandName { get; } = commandName;

    /// <summary>
    /// Gets the full selected slice path.
    /// </summary>
    public string FullSlicePath { get; } = fullSlicePath;

    /// <summary>
    /// Gets the exact source location supplied on the code block.
    /// </summary>
    public SourceLocation Location { get; } = location;

    /// <summary>
    /// Gets the reason admission failed.
    /// </summary>
    public InlineCommandHandlerRejectionReason Reason { get; } = reason;
}
