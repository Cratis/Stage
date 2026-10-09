// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_entries_in_another_order : given.a_source_plan
{
    CratisPlanResult _original = null!;
    PlanSelection _selection = new([PlanSelectionEntry.Module("Projects"), PlanSelectionEntry.Module("Tasks")]);
    void Establish() => _original = CratisRendering.PlanFrom(_loaded, _selection, _planOptions);
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([.. _selection.Entries.Reverse()]), _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_preserve_the_digest() => _result.Digest.ShouldEqual(_original.Digest);
}
