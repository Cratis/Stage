// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// The exception that is thrown when an in-memory append fails.
/// </summary>
/// <param name="message">The append failure.</param>
public sealed class SemanticAppendFailed(string message) : Exception(message);

/// <summary>
/// The exception that is thrown when an expression escaped admission.
/// </summary>
public sealed class UnsupportedSemanticMapping : Exception
{
    /// <summary>
    /// Creates the failure when a mapping escapes admission.
    /// </summary>
    public UnsupportedSemanticMapping() : base("A mapping escaped semantic admission.")
    {
    }
}
