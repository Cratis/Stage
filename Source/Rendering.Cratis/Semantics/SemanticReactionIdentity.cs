// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Decides whether a reaction needs a rendered default identity to invoke its commands.
/// </summary>
internal static class SemanticReactionIdentity
{
    /// <summary>
    /// Gets a value indicating whether a reaction that declares no <c language="csharp">runs as</c> invokes a command
    /// with no modeled authorization.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="reaction">The reaction to inspect.</param>
    /// <returns><see langword="true"/> when the reactor needs the system identity to be accepted.</returns>
    public static bool InvokesUnauthorizedCommand(SemanticApplicationContext context, SemanticReaction reaction) =>
        reaction.RunsAs is null && InvokedCommands(context, reaction).Any(command => command.Authorization is null);

    /// <summary>
    /// Gets the distinct commands with modeled authorization that a reaction invokes.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="reaction">The reaction to inspect.</param>
    /// <returns>The authorized commands, in invocation order.</returns>
    public static IEnumerable<SemanticCommand> AuthorizedCommands(SemanticApplicationContext context, SemanticReaction reaction) =>
        InvokedCommands(context, reaction).Where(command => command.Authorization is not null);

    static IEnumerable<SemanticCommand> InvokedCommands(SemanticApplicationContext context, SemanticReaction reaction) =>
        reaction.Triggers.SelectMany(trigger => trigger.Invokes)
            .Select(invocation => context.Commands[invocation.Command])
            .DistinctBy(command => command.Id);
}
