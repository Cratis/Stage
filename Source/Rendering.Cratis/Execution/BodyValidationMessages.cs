// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

internal static class BodyValidationMessages
{
    internal static BodyEvaluation Read(object? result)
    {
        if (result is not IEnumerable<string> iterator) return Failure("Body validation returned no message iterator.");
        try
        {
            var messages = ImmutableArray.CreateBuilder<string>();
            foreach (var message in iterator)
            {
                if (InvalidMessage(message) || messages.Count == 100) return Failure("Body validation messages exceeded their bounds.");
                messages.Add(message);
            }
            return new(messages.Count == 0 ? "satisfied" : "unsatisfied", Messages: messages.ToImmutable());
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or InsufficientExecutionStackException))
        {
            return Failure("Body validation iterator failed.");
        }
    }

    static bool InvalidMessage(string? message) => message is null || message.Length > 1024;

    static BodyEvaluation Failure(string message) => new("faulted", Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, message));
}
