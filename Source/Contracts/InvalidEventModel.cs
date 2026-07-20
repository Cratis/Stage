// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// The exception that is thrown when the Screenplay source for an event model is missing or cannot be compiled into a model.
/// </summary>
public class InvalidEventModel : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEventModel"/> class.
    /// </summary>
    /// <param name="path">The path to the source that could not be loaded.</param>
    public InvalidEventModel(string path)
        : base($"The event model at '{path}' could not be read as a valid event model.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEventModel"/> class with the compiler diagnostics that
    /// explain the failure.
    /// </summary>
    /// <param name="path">The path to the source that could not be compiled.</param>
    /// <param name="diagnostics">The diagnostic messages produced during compilation.</param>
    public InvalidEventModel(string path, IEnumerable<string> diagnostics)
        : base(BuildMessage(path, diagnostics))
    {
    }

    static string BuildMessage(string path, IEnumerable<string> diagnostics)
    {
        var messages = diagnostics.ToArray();

        return messages.Length == 0
            ? $"The event model at '{path}' could not be read as a valid event model."
            : $"The event model at '{path}' could not be compiled:{Environment.NewLine}{string.Join(Environment.NewLine, messages)}";
    }
}
