// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>The bounds a rejecting validation's messages must respect before they reach a caller.</summary>
internal static class BodyValidationBounds
{
    /// <summary>The maximum number of messages one validation may yield.</summary>
    public const int MaximumMessages = 100;

    /// <summary>The maximum length of one message.</summary>
    public const int MaximumMessageLength = 1024;

    /// <summary>Checks that a rejection carries at least one bounded, non-null message.</summary>
    /// <param name="messages">The yielded messages, possibly default or containing nulls.</param>
    /// <returns>Whether the messages are a usable rejection.</returns>
    public static bool Valid(ImmutableArray<string> messages) =>
        !messages.IsDefaultOrEmpty &&
        messages.Length <= MaximumMessages &&
        messages.All(message => message?.Length <= MaximumMessageLength);
}
