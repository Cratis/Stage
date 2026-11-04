// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

public sealed partial class SceneRuntimeAdmission
{
    GuardSubject? _usedSubject;
    GuardSubject? _subject;

    /// <inheritdoc/>
    public override void VisitBehavior(BehaviorSyntax syntax)
    {
        // Reusable declarations are checked at each use site, never at their declaration.
        if (_scope?.Declared.Values.Contains(syntax) == true) return;
        base.VisitBehavior(syntax);
    }

    /// <inheritdoc/>
    public override void VisitUsesBehavior(UsesBehaviorSyntax syntax)
    {
        if (_scope?.Declared.TryGetValue(syntax.Behavior, out var behavior) == true)
        {
            var previous = _usedSubject;
            _usedSubject = _scope.GuardContexts?.At(syntax);
            try
            {
                base.VisitBehavior(behavior);
            }
            finally
            {
                _usedSubject = previous;
            }
        }

        base.VisitUsesBehavior(syntax);
    }
}
