// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticHandleLocals;

public class when_allocating_production_names : Specification
{
    static SemanticHandleLocals Locals()
    {
        var model = invoice_model.Compile(when_rendering_conditional_generated_productions.Source);
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(candidate => candidate.Name == "Create");
        return new SemanticHandleLocals(command);
    }

    [Fact]
    public void should_not_alias_a_named_local()
    {
        var locals = Locals();
        locals.Name("occurred").ShouldEqual("occurred");
        locals.Production(0, "occurred").ShouldEqual("occurred1");
        locals.Name("occurred").ShouldEqual("occurred");
    }

    [Fact]
    public void should_reuse_a_position_and_suffix_a_clashing_one()
    {
        var locals = Locals();
        locals.Production(1, "orderPlaced").ShouldEqual("orderPlaced");
        locals.Production(1, "orderPlaced").ShouldEqual("orderPlaced");
        locals.Production(2, "orderPlaced").ShouldEqual("orderPlaced1");
        locals.Name("orderPlaced").ShouldEqual("orderPlaced2");
    }
}
