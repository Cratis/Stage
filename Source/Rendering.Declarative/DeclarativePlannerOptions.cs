// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents the limits of one declarative planner.
/// </summary>
/// <param name="Budget">The template budget of each plan; the profile default when <see langword="null"/>.</param>
/// <param name="KitLimits">The kit loading limits; the loader default when <see langword="null"/>.</param>
public sealed record DeclarativePlannerOptions(TemplateBudget? Budget = null, KitLoadLimits? KitLimits = null);
