// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// A named value: a header, a query parameter or a value an ability returns.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Value">The <see cref="HttpValue"/>.</param>
public sealed record HttpNamedValue(string Name, HttpValue Value);
