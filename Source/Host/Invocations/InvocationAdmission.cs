// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The answer to a request to invoke an ability.
/// </summary>
/// <param name="Admitted">Whether an invocation exists for the request, newly or from an earlier delivery of it.</param>
/// <param name="Record">The record of the invocation, when admitted.</param>
/// <param name="Refusal">Why it was not admitted.</param>
public sealed record InvocationAdmission(bool Admitted, InvocationRecord? Record, string Refusal);
