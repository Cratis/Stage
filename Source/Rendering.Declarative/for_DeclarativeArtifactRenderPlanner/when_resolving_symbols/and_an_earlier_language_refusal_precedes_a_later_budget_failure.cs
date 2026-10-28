// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_an_earlier_language_refusal_precedes_a_later_budget_failure : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new();
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _language.RefusedPaths["Commands/Echo.test"] = given.a_test_language.AmbiguousName;
        var imports = "{{ sym.ref 'Some.Long.Namespace.Name' 'Thing' }}" + string.Concat(Enumerable.Repeat("{{ sym.imports }}", 200));
        _plan = PlanWithLanguage(new() { ["templates/index.sbn"] = imports }, _language, TemplateBudget.Default with { MaxOutputBytes = 2000 });
    }

    [Fact] void should_report_the_language_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([given.a_test_language.AmbiguousName]);
}
