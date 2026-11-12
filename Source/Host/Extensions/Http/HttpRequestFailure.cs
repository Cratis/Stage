// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Thrown inside the extension when a request cannot be built; nothing was sent.
/// </summary>
/// <param name="reason">Why, without any value that could be a secret.</param>
public sealed class HttpRequestFailure(string reason) : Exception(reason);
