// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a composition declaration carries a value Stage has no Scene translation for.
/// </summary>
/// <param name="kind">What the value is, for example a collection operation or a template scope.</param>
/// <param name="value">The value.</param>
public sealed class UnknownCompositionValue(string kind, string value) : Exception($"Unknown {kind} '{value}' - it has no Scene translation.");
