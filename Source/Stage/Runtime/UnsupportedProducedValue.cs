// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when a produced event property refers to a value the Stage runtime cannot resolve.
/// </summary>
/// <param name="expression">The unsupported modeled expression.</param>
/// <param name="property">The event property that would be omitted.</param>
public sealed class UnsupportedProducedValue(string expression, string property)
    : Exception($"Cannot produce event property '{property}' from unsupported expression '$context.{expression}'.");
