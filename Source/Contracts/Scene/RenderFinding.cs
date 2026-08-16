// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// One thing a <see cref="RenderPlan"/> could not fully resolve - part of Cratis/Stage#39.
/// </summary>
/// <remarks>
/// A finding is reported, never thrown. Resolution keeps going and the plan is returned complete with
/// everything that <em>did</em> resolve, so a caller sees every problem in one pass instead of the first one
/// only - and so a build can decide for itself whether a given finding stops it. What must not happen is a
/// half-resolved target shipping silently, which is why the plan carries the finding rather than dropping the
/// unresolved part.
/// </remarks>
/// <param name="Kind">What the finding is about.</param>
/// <param name="Subject">The name the finding is about - the package, component, screen, template, theme or layout.</param>
/// <param name="Message">A one-line explanation, naming the target it applies to.</param>
public record RenderFinding(RenderFindingKind Kind, string Subject, string Message);
