// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Refuses the UI members the Scene converters do not translate. Shared by render planning and Host scene loading.
/// </summary>
/// <remarks>
/// Package components, toolbars, UI bindings, template metadata, slots and the outlets of layouts, screen templates and dialog templates
/// are translated by the converters, and so is the typed composition ABI of Screenplay 4.120: exposures, instance values,
/// screen contributions, template slot content and picker metadata, grid/grow/span arrangements, and a
/// <c>navigate</c> directive's route, slot and parameters (as a Scene <c>DestinationReference</c>), and a navigation
/// contribution's id, icon, presentation, group and destination. Since Screenplay 6 and Scene 5 a dialog template's
/// <c>outlet actions of actions</c> is a Scene outlet and is translated. Two Screenplay 6 members are not translated yet and are
/// refused rather than dropped: <c>show &lt;Screen&gt; by &lt;param&gt; from &lt;element&gt;.selection</c> (a binding between components) and a
/// form field's <c>choose from query</c> picker.
/// </remarks>
public sealed class UiSyntaxAdmission : ScreenplaySyntaxWalker
{
    readonly ConversionDiagnostics? _diagnostics;

    /// <summary>
    /// Creates an admission check with legacy throwing behavior.
    /// </summary>
    public UiSyntaxAdmission()
    {
    }

    /// <summary>
    /// Creates an admission check that reports unsupported UI members.
    /// </summary>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    public UiSyntaxAdmission(ConversionDiagnostics? diagnostics) => _diagnostics = diagnostics;

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        new Screenplay.ActionSubjectSyntaxAdmission().VisitNode(node);
        var (member, path) = Refused(node);
        if (member is null) return;
        if (_diagnostics is null) throw new UnsupportedUiSyntax(member, node.Location);
        _diagnostics.Report(new(UnsupportedUiSyntax.DiagnosticCode, DiagnosticSeverity.Error, member, path, node.Location, $"Unsupported {member} was omitted from '{path}'."));
    }

    static (string? Member, string Path) Refused(SyntaxNode node) => node switch
    {
        ScreenShowSyntax show => ("ScreenShowSyntax", show.Screen),
        FormFieldSyntax { Choices: not null } field => ("FormFieldSyntax.Choices", field.Property),
        _ => (null, string.Empty)
    };
}
