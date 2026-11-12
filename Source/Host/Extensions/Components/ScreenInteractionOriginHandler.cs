// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Serves the invocations a person starts from an extension component. The browser asks for the outcome on its own request, so settling has nowhere
/// else to take it.
/// </summary>
public sealed class ScreenInteractionOriginHandler : IInvocationOriginHandler
{
    /// <inheritdoc/>
    public InvocationOriginKind Kind => InvocationOriginKind.ScreenInteraction;

    /// <inheritdoc/>
    public Task Settled(InvocationRecord record) => Task.CompletedTask;
}
