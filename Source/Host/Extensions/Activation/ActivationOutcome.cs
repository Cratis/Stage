// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The result of asking for a backend to run.
/// </summary>
/// <param name="Endpoint">Where the backend is, or null when it is not running.</param>
/// <param name="Refusal">Why it is not running, or empty when it is or when the system has no backend.</param>
public sealed record ActivationOutcome(ExtensionEndpoint? Endpoint, string Refusal = "");
