// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Interactions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Moves successful-submit actions to Scene's form continuation, exactly once.
/// </summary>
internal static class FormSubmitConverter
{
    internal static Form Convert(FormSyntax syntax, BehaviorScope scope, string path)
    {
        var form = FormConverter.Convert(syntax, scope.Diagnostics, path);
        var responses = new FormResponseBindings { Commands = scope.CommandResponses }.Without(syntax.Fields.Select(field => field.Property)).For(syntax.For);
        var actions = form.OnSubmit.Select(action => TryAction(responses, action, scope, syntax, path) ?? action).ToList();
        var retained = new List<Behavior>();
        foreach (var behavior in scope.Resolve(syntax.Behaviors, syntax.UsedBehaviors, syntax.Name))
        {
            var declaration = syntax.Behaviors?.FirstOrDefault(candidate => candidate.Name == behavior.Name) ?? (behavior.Name is { } name ? scope.Declared.GetValueOrDefault(name) : null);
            var shadowed = declaration?.Parameters.Select(parameter => parameter.Name) ?? [];
            var values = responses.Without(shadowed);
            var kept = new List<InteractionBinding>(behavior.Bindings.Where(binding => !IsSubmit(binding)));
            foreach (var binding in behavior.Bindings.Where(IsSubmit))
            {
                if (binding.Condition is not null)
                {
                    // A guarded submit has no place in the form continuation: keep it as the plain behavior it was.
                    Refuse(scope, syntax, path, DiagnosticSeverity.Warning, "STAGE-SCENE-FORM-002", "guarded form-submit behavior", "Guarded form-submit behaviors cannot be represented by the pinned Scene form continuation; the guarded behavior was kept as a behavior");
                    kept.Add(binding);
                    continue;
                }

                var translated = new List<InteractionAction>();
                foreach (var action in binding.Actions)
                {
                    var result = TryAction(values, action, scope, syntax, path);
                    if (result is null) break;
                    translated.Add(result);
                }

                if (translated.Count == binding.Actions.Count) actions.AddRange(translated);
                else kept.Add(binding);
            }
            retained.Add(behavior with { Bindings = kept });
        }
        return form with { OnSubmit = actions, Behaviors = [.. retained.Where(behavior => behavior.Bindings.Count > 0)] };
    }

    static InteractionAction? TryAction(FormResponseBindings responses, InteractionAction action, BehaviorScope scope, FormSyntax syntax, string path)
    {
        try
        {
            return responses.Action(action);
        }
        catch (UnsupportedFormResponseCapture exception)
        {
            Refuse(scope, syntax, path, DiagnosticSeverity.Error, "STAGE-SCENE-FORM-003", "form response capture", exception.Message + "; the submit actions were kept as a behavior");
            return null;
        }
    }

    static void Refuse(BehaviorScope scope, FormSyntax syntax, string path, DiagnosticSeverity severity, string code, string construct, string message)
    {
        // The kept behavior is a faithful translation, so a legacy caller without a collector loses nothing and must not crash.
        scope.Diagnostics?.Report(new(code, severity, construct, path, syntax.Location, $"{message} in '{path}'."));
    }

    static bool IsSubmit(InteractionBinding binding) => binding.Trigger is BuiltInInteractionTrigger { Kind: Cratis.Scene.Model.Interactions.InteractionTriggerKind.Submit };
}
