// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// The result of reading an <c lang="text">http</c> block.
/// </summary>
/// <param name="Definition">The <see cref="HttpAbilityDefinition"/>, or null when the block has errors.</param>
/// <param name="Errors">The errors, each naming its line.</param>
public sealed record HttpBlockParse(HttpAbilityDefinition? Definition, IReadOnlyList<string> Errors);
