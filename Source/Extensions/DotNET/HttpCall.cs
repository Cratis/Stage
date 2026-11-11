// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Extensions;

/// <summary>
/// An inbound HTTP request Stage routed to the extension.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path below the extension's mount, starting with a slash.</param>
/// <param name="Query">The raw query string without the question mark.</param>
/// <param name="Headers">The headers. Cookies and authorization are never forwarded.</param>
/// <param name="Body">The body, exactly as it arrived, so a signature computed over it verifies.</param>
/// <param name="DeliveryId">The identity of the stored request. It is the same for every attempt, so the extension can deduplicate on it; empty when the call was not stored first.</param>
/// <param name="Attempt">The attempt number, starting at 1; zero when the call was not stored first.</param>
public sealed record HttpCall(string Method, string Path, string Query, IReadOnlyDictionary<string, string[]> Headers, ReadOnlyMemory<byte> Body, string DeliveryId = "", int Attempt = 0);

/// <summary>
/// The answer to an <see cref="HttpCall"/>.
/// </summary>
/// <param name="Status">The HTTP status.</param>
/// <param name="Body">The body.</param>
/// <param name="Headers">The headers.</param>
public sealed record HttpReply(int Status, ReadOnlyMemory<byte> Body = default, IReadOnlyDictionary<string, string[]>? Headers = null);
