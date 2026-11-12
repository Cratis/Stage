// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// What caused a backend to be started.
/// </summary>
public enum ActivationReason
{
    /// <summary>
    /// A call needed it: an ability, an inbound delivery or a proxied request.
    /// </summary>
    Invocation,

    /// <summary>
    /// The browser reached a screen that places one of the extension's components.
    /// </summary>
    Navigation,

    /// <summary>
    /// The system's policy keeps the backend running.
    /// </summary>
    AlwaysOn
}
