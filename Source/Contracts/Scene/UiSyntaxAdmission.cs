// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Refuses the UI surface added in Screenplay 4.94–4.101 until Stage can translate it faithfully.
/// Shared by render planning, Host scene loading and legacy rendering.
/// </summary>
public sealed class UiSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is InteractionBindingSyntax binding && (binding.Alternatives.Any() || binding.Otherwise is not null))
        {
            throw new UnsupportedGuardedInteraction(binding.Location);
        }

        var member = node switch
        {
            ApplicationSyntax application when application.Templates.Any() => "ApplicationSyntax.Templates",
            ModuleSyntax module when module.Templates.Any() => "ModuleSyntax.Templates",
            FeatureSyntax feature when feature.Templates.Any() => "FeatureSyntax.Templates",
            SliceSyntax slice when slice.Templates.Any() => "SliceSyntax.Templates",
            ScreenComponentSyntax => nameof(ScreenComponentSyntax),
            ScreenToolbarSyntax => nameof(ScreenToolbarSyntax),
            UiBindingSyntax => nameof(UiBindingSyntax),
            ScreenNavigateSyntax navigate when navigate.Route is not null || navigate.Parameters.Any() => "ScreenNavigateSyntax.Route/Parameters",
            FormSyntax form when form.ColumnMode != FormColumnMode.Unspecified || form.Columns.Any() => "FormSyntax.ColumnMode/Columns",
            LayoutSyntax layout when layout.Category is not null || layout.TemplateType is not null || layout.Exposes.Any() || layout.Outlets.Any() => "LayoutSyntax.Category/TemplateType/Exposes/Outlets",
            ScreenTemplateSyntax template when template.Category is not null || template.TemplateType is not null || template.Exposes.Any() || template.Outlets.Any() => "ScreenTemplateSyntax.Category/TemplateType/Exposes/Outlets",
            DialogTemplateSyntax dialog when dialog.Category is not null || dialog.TemplateType is not null || dialog.Exposes.Any() || dialog.Outlets.Any() => "DialogTemplateSyntax.Category/TemplateType/Exposes/Outlets",
            UiProfileSyntax profile when profile.Icons.Any() => "UiProfileSyntax.Icons",
            _ => null
        };
        if (member is not null)
        {
            throw new UnsupportedUiSyntax(member, node.Location);
        }
    }
}
