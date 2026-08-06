// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// The exception that is thrown when a condition has no C# rendering. Rendering it as a true literal would compile
/// into an always-passing guard — the guarded behavior would run unconditionally — so the slice fails loudly
/// instead.
/// </summary>
/// <param name="condition">The <see cref="ConditionSyntax"/> that could not be rendered.</param>
public class UnsupportedCondition(ConditionSyntax condition)
    : Exception($"Condition of type '{condition.GetType().Name}' at line {condition.Location.Line} has no C# rendering.");
