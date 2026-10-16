// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticHandleLocals;

public class when_allocating_clashing_names : Specification
{
    [Theory]
    [InlineData("operation")]
    [InlineData("occurred")]
    [InlineData("stageEvents")]
    [InlineData("stageProduces0")]
    [InlineData("streamIdPart0")]
    [InlineData("source")]
    public void should_use_the_first_free_suffix_and_reuse_it_for_every_reference(string preferred)
    {
        var model = invoice_model.Compile(when_rendering_conditional_generated_productions.Source);
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(candidate => candidate.Name == "Create");
        var generated = 0;
        var renamed = command with
        {
            Properties = [.. command.Properties.Select(property => property.IsGenerated
                ? property with { Name = generated++ == 0 ? preferred : $"{preferred}1" }
                : property)]
        };
        var locals = new SemanticHandleLocals(renamed);
        locals.Name(preferred).ShouldEqual($"{preferred}2");
        locals.Name(preferred).ShouldEqual($"{preferred}2");
        locals.Name("unclaimed").ShouldEqual("unclaimed");
        locals.Name($"{preferred}2").ShouldEqual($"{preferred}21");
    }
}
