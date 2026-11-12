// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The backend of a system could not be started.
/// </summary>
/// <param name="message">What went wrong, for the person who runs the deployment.</param>
/// <param name="innerException">The cause, when there is one.</param>
public sealed class BackendStartException(string message, Exception? innerException = null) : Exception(message, innerException);
