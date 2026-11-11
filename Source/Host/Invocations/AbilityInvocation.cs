// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// A request to invoke an ability.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Ability">The name of the ability.</param>
/// <param name="Origin">The <see cref="InvocationOrigin"/>, which gives the invocation its identity.</param>
/// <param name="Input">The input of the ability, as JSON.</param>
/// <param name="AnswerPolicy">The policy the identity that answers must satisfy, for an ability that answers later. Any authenticated identity may answer when empty.</param>
public sealed record AbilityInvocation(string System, string Ability, InvocationOrigin Origin, string Input = "{}", string AnswerPolicy = "");
