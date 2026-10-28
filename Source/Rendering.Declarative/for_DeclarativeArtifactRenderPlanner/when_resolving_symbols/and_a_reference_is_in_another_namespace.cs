// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_a_reference_is_in_another_namespace : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/command.sbn"] = "{{ sym.imports }}|{{ sym.decl 'Domain' item.name }}|{{ sym.ref 'Other' 'Thing' }}" });

    [Fact] void should_import_the_namespace_and_use_the_short_name() => Text(_plan, "Commands/Echo.test").ShouldEqual("using Other;\n|Echo|Thing");
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
}
