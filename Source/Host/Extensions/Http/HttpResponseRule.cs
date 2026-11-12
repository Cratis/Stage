// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// What an ability does with one response status.
/// </summary>
/// <param name="Status">The HTTP status code.</param>
/// <param name="Outcome">The outcome the ability settles with, or null when the call returned normally.</param>
/// <param name="Output">The values the ability returns, read from the response.</param>
public sealed record HttpResponseRule(int Status, string? Outcome, IReadOnlyList<HttpNamedValue> Output);
