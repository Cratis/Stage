// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// How to reach a system over HTTP.
/// </summary>
/// <param name="BaseAddress">The base address, or null when it cannot be used.</param>
/// <param name="Authenticate">Adds the credentials to a request.</param>
/// <param name="Refusal">Why the system cannot be reached; it names a setting and never holds a value.</param>
public sealed record HttpSystemConnection(Uri? BaseAddress, Action<HttpRequestMessage> Authenticate, string? Refusal);
