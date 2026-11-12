// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// A field of the request body: either one value or an object of further fields.
/// </summary>
/// <param name="Name">The name of the field.</param>
/// <param name="Value">The value, or null when the field is an object.</param>
/// <param name="Fields">The fields of the object.</param>
public sealed record HttpBodyField(string Name, HttpValue? Value, IReadOnlyList<HttpBodyField> Fields);
