// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_the_output_is_inspected : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/command.sbn"] = "{{ sym.imports }}|{{ sym.decl 'Domain' item.name }}|{{ sym.ref 'Other' 'Thing' }}|{{ sym.ref 'System' 'Guid' }}" });

    [Fact] void should_render_every_artifact() => _plan.Artifacts.Length.ShouldEqual(4);
    [Fact] void should_not_spell_any_declared_symbol_qualified() => _plan.Artifacts.Select(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())).Any(_ => _.Contains("Other.Thing", StringComparison.Ordinal) || _.Contains("System.Guid", StringComparison.Ordinal) || _.Contains("Domain.", StringComparison.Ordinal)).ShouldBeFalse();
}
