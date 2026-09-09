// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a legacy command handler references an unsupported file-backed implementation.
/// </summary>
/// <param name="commandName">The authored command name.</param>
/// <param name="fullSlicePath">The full selected slice path.</param>
/// <param name="commandSourceLocation">The exact source location of the command declaration.</param>
/// <param name="file">The symbolic implementation reference, including its exact source location.</param>
public class UnsupportedFileBackedCommandHandler(
    string commandName,
    string fullSlicePath,
    SourceLocation commandSourceLocation,
    FileReferenceSyntax file)
    : Exception(
        $"{DiagnosticCode}: Command '{commandName}' in slice '{fullSlicePath}' at {commandSourceLocation.Path}({commandSourceLocation.Line},{commandSourceLocation.Column}): " +
        "file-backed command implementation not supported by legacy Cratis renderer. " +
        $"Implementation reference '{file.Path}' at {file.Location.Path}({file.Location.Line},{file.Location.Column}).")
{
    /// <summary>
    /// The stable diagnostic code for legacy file-backed command handler rejection.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-FILE-001";

    /// <summary>
    /// Gets the authored command name.
    /// </summary>
    public string CommandName { get; } = commandName;

    /// <summary>
    /// Gets the full selected slice path.
    /// </summary>
    public string FullSlicePath { get; } = fullSlicePath;

    /// <summary>
    /// Gets the exact source location supplied on the command declaration.
    /// </summary>
    public SourceLocation CommandSourceLocation { get; } = commandSourceLocation;

    /// <summary>
    /// Gets the symbolic implementation reference and its exact authored source location; no file lookup is performed.
    /// </summary>
    public FileReferenceSyntax File { get; } = file;
}
