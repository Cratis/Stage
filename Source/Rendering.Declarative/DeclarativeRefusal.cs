// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents a language-owned safety refusal that stops the whole plan.
/// </summary>
/// <param name="Code">The stable language-owned code.</param>
/// <param name="Message">The reason, without secrets.</param>
public sealed record DeclarativeRefusal(string Code, string Message);
