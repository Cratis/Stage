// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Extensions;

/// <summary>
/// A host service refused what the extension asked for: a setting it did not declare, a secret asked for as a setting, or host services used before the handshake.
/// </summary>
/// <param name="message">What was refused.</param>
public class HostServiceException(string message) : Exception(message);
