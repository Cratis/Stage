// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Interactions;
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
        var actions = form.OnSubmit.Select(responses.Action).ToList();
        var retained = new List<Behavior>();
        foreach (var behavior in scope.Resolve(syntax.Behaviors, syntax.UsedBehaviors, syntax.Name))
        {
            var declaration = syntax.Behaviors?.FirstOrDefault(candidate => candidate.Name == behavior.Name) ?? (behavior.Name is { } name ? scope.Declared.GetValueOrDefault(name) : null);
            var shadowed = declaration?.Parameters.Select(parameter => parameter.Name) ?? [];
            var values = responses.Without(shadowed);
            foreach (var binding in behavior.Bindings.Where(IsSubmit))
            {
                if (binding.Condition is not null) throw new NotSupportedException("Guarded form-submit behaviors cannot be represented by the pinned Scene form continuation.");
                actions.AddRange(binding.Actions.Select(values.Action));
            }
            retained.Add(behavior with { Bindings = [.. behavior.Bindings.Where(binding => !IsSubmit(binding))] });
        }
        return form with { OnSubmit = actions, Behaviors = [.. retained.Where(behavior => behavior.Bindings.Count > 0)] };
    }

    static bool IsSubmit(InteractionBinding binding) => binding.Trigger is BuiltInInteractionTrigger { Kind: Cratis.Scene.Model.Interactions.InteractionTriggerKind.Submit };
}
