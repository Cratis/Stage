// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http.given;

/// <summary>
/// A request the test server received.
/// </summary>
/// <param name="Method">The method.</param>
/// <param name="Target">The path and query as received.</param>
/// <param name="Headers">The headers.</param>
/// <param name="Body">The body.</param>
public sealed record RecordedRequest(string Method, string Target, IReadOnlyDictionary<string, string> Headers, string Body);
