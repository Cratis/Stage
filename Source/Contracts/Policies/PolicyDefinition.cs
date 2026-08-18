// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Policies;

/// <summary>
/// Represents a <c>policy</c> declared by the application - the named, reusable authorization rule a command's
/// <c>authorize</c> declaration refers to by name.
/// </summary>
/// <param name="Id">The unique identifier of the policy.</param>
/// <param name="Name">The name of the policy.</param>
/// <param name="Condition">What the policy requires, or <see langword="null"/> when it states no condition -
/// which is the case for a policy implemented in code, see <paramref name="CodeLanguage"/>.</param>
/// <param name="CodeLanguage">The language tag of the inline code block implementing the policy, or an empty
/// string when the policy states its condition declaratively.</param>
/// <remarks>
/// <see cref="Commands.AuthorizationRequirement"/> holds the names a command requires; this holds what those
/// names mean. Without it a consumer can list which policies guard a command but cannot say what any of them
/// checks.
/// <para>
/// Only the language tag of an inline implementation is carried, never the code itself. The contract states
/// the model, not its realization - the same line <see cref="Commands.CommandDefinition"/> draws by carrying
/// a <c>LogicDescription</c> and no body. The tag is carried so a policy that is implemented in code stays
/// distinguishable from one that requires nothing at all.
/// </para>
/// </remarks>
public record PolicyDefinition(
    Guid Id,
    string Name,
    PolicyCondition? Condition,
    string CodeLanguage);
