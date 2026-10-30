// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses explicit action subject inputs that Stage cannot yet render, before a syntax path can drop them silently.
/// </summary>
/// <remarks>
/// An explicit subject itself is rendered (Screenplay decision 0110). The only inputs rendered are <c>with &lt;name&gt; from item.&lt;path&gt;</c>
/// on a <c>for item</c> action; any other input of an action that names a subject is refused with STAGE-ESM-059.
/// </remarks>
public sealed class ActionSubjectSyntaxAdmission : ScreenplaySyntaxWalker
{
    const string ItemPrefix = "item.";

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is not ScreenActionSyntax { Subject: not null } action)
        {
            return;
        }

        foreach (var input in action.Inputs.Where(input => !IsRendered(action.Subject, input)))
        {
            throw new InvalidEventModel(
                node.Location.Path ?? "<source>",
                [$"STAGE-ESM-059: Screen action '{action.Command}' input '{input.Name}' from '{input.Binding}' at {node.Location} is not yet rendered; only 'from item.<path>' on 'for item' actions is (Cratis/Stage#855, Screenplay decision 0110)."]);
        }
    }

    static bool IsRendered(ScreenActionSubjectKind? subject, InteractionArgumentSyntax input) =>
        subject == ScreenActionSubjectKind.Item
        && input.Binding.StartsWith(ItemPrefix, StringComparison.Ordinal)
        && input.Binding.Length > ItemPrefix.Length;
}
