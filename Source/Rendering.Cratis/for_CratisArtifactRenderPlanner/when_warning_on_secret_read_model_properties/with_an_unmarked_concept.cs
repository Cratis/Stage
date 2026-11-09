// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_warning_on_secret_read_model_properties;

public class with_an_unmarked_concept : given.a_secret_read_model_plan
{
    void Because() => _plan = invoice_model.Plan(Compile(marked: false));

    [Fact] void should_not_report_the_warning() => Warnings.ShouldBeEmpty();
}
