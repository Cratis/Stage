// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// The <c lang="text">http</c> realization of one ability: the request to make and what each response means.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Ability">The name of the ability.</param>
/// <param name="Method">The HTTP method, upper case.</param>
/// <param name="Path">The path template, relative to the base address of the system; <c lang="text">{input}</c> is replaced by an input, escaped.</param>
/// <param name="Headers">The headers to send.</param>
/// <param name="Query">The query parameters to send.</param>
/// <param name="Body">The fields of the body, empty when there is none.</param>
/// <param name="Responses">What each status means.</param>
public sealed record HttpAbilityDefinition(
    string System,
    string Ability,
    string Method,
    string Path,
    IReadOnlyList<HttpNamedValue> Headers,
    IReadOnlyList<HttpNamedValue> Query,
    IReadOnlyList<HttpBodyField> Body,
    IReadOnlyList<HttpResponseRule> Responses);
