// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

public sealed partial class SceneRuntimeAdmission
{
    /// <inheritdoc/>
    public override void VisitScreen(ScreenSyntax syntax) => InScope(syntax.Directives, () => base.VisitScreen(syntax));

    /// <inheritdoc/>
    public override void VisitScreenSection(ScreenSectionSyntax syntax) => InScope(syntax.Directives, () => base.VisitScreenSection(syntax));

    /// <inheritdoc/>
    public override void VisitScreenSlot(ScreenSlotSyntax syntax) => InScope(syntax.Directives, () => base.VisitScreenSlot(syntax));

    /// <inheritdoc/>
    public override void VisitBehavior(BehaviorSyntax syntax)
    {
        // A reusable declaration is checked in the data scope of each attachment, not where it is declared.
        if (_scope?.Subject is null && _scope?.Declared.Values.Contains(syntax) == true) return;
        base.VisitBehavior(syntax);
    }

    /// <inheritdoc/>
    public override void VisitUsesBehavior(UsesBehaviorSyntax syntax)
    {
        if (_scope?.Declared.TryGetValue(syntax.Behavior, out var behavior) == true) base.VisitBehavior(behavior);
        base.VisitUsesBehavior(syntax);
    }

    void InScope(IEnumerable<ScreenDirectiveSyntax> directives, Action visit)
    {
        var previous = _scope;
        if (_scope is not null)
        {
            _scope = _scope with { Subject = directives.OfType<ScreenDataSyntax>().LastOrDefault()?.Type.Name ?? _scope.Subject };
        }

        try
        {
            visit();
        }
        finally
        {
            _scope = previous;
        }
    }
}
