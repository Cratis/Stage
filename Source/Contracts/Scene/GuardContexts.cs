// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>Indexes each authored attachment once, including distinct uses of reusable behaviors.</summary>
internal sealed class GuardContexts : ScreenplaySyntaxWalker
{
    readonly Dictionary<SyntaxNode, GuardSubject> _subjects = new(ReferenceEqualityComparer.Instance);
    GuardSubject _current;

    internal GuardContexts(ApplicationSyntax syntax)
    {
        _current = new(new GuardDeclarations(syntax), [], []);
        VisitApplication(syntax);
    }

    public override void VisitNode(SyntaxNode node) => _subjects[node] = _current;

    public override void VisitModule(ModuleSyntax syntax) => In(_current with { Scope = [.. _current.Scope, syntax.Name] }, () => base.VisitModule(syntax));

    public override void VisitFeature(FeatureSyntax syntax) => In(_current with { Scope = [.. _current.Scope, syntax.Name] }, () => base.VisitFeature(syntax));

    public override void VisitSlice(SliceSyntax syntax) => In(_current with { Scope = [.. _current.Scope, syntax.Name] }, () => base.VisitSlice(syntax));

    public override void VisitScreen(ScreenSyntax syntax)
    {
        In(_current.Enter(syntax.Directives, true), () =>
        {
            VisitNode(syntax);
            foreach (var directive in syntax.Directives) VisitScreenDirective(directive);
        });
        foreach (var contribution in syntax.Contributions)
        {
            In(_current.Enter(contribution.Directives, true), () =>
            {
                VisitNode(contribution);
                foreach (var directive in contribution.Directives) VisitScreenDirective(directive);
            });
        }
    }

    public override void VisitScreenSection(ScreenSectionSyntax syntax) => In(_current.Enter(syntax.Directives), () => base.VisitScreenSection(syntax));

    public override void VisitScreenSlot(ScreenSlotSyntax syntax) => In(_current.Enter(syntax.Directives), () => base.VisitScreenSlot(syntax));

    public override void VisitScreenTable(ScreenTableSyntax syntax) => In(_current.Row(syntax.Target), () => base.VisitScreenTable(syntax));

    public override void VisitTemplateSlotContent(TemplateSlotContentSyntax syntax) => In(_current.Enter(syntax.Directives, true), () => base.VisitTemplateSlotContent(syntax));

    public override void VisitScreenComponent(ScreenComponentSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var behavior in syntax.Behaviors) VisitBehavior(behavior);
        foreach (var uses in syntax.UsedBehaviors) VisitUsesBehavior(uses);
        foreach (var outlet in syntax.Outlets)
        {
            In(_current.Enter(outlet.Directives), () =>
            {
                VisitNode(outlet);
                foreach (var directive in outlet.Directives) VisitScreenDirective(directive);
            });
        }
    }

    public override void VisitScreenTemplate(ScreenTemplateSyntax syntax) => In(_current with { Candidates = [] }, () => base.VisitScreenTemplate(syntax));

    public override void VisitDialogTemplate(DialogTemplateSyntax syntax) => In(_current with { Candidates = [] }, () => base.VisitDialogTemplate(syntax));

    public override void VisitForm(FormSyntax syntax) => In(_current with { Candidates = [] }, () => base.VisitForm(syntax));

    public override void VisitScreenToolbar(ScreenToolbarSyntax syntax) => In(_current with { Candidates = [] }, () => base.VisitScreenToolbar(syntax));

    internal GuardSubject? At(SyntaxNode site) => _subjects.GetValueOrDefault(site);

    void In(GuardSubject subject, Action visit)
    {
        var previous = _current;
        _current = subject;
        try
        {
            visit();
        }
        finally
        {
            _current = previous;
        }
    }
}
