// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_sources_change_after_startup : a_specification_endpoint
{
    async Task Because()
    {
        await File.WriteAllTextAsync(_modelPath, specification_plan.Source(seeded: true));
        await Request("{}");
    }

    [Fact] void should_not_accept_results_for_another_model_revision() => _status.ShouldEqual(StatusCodes.Status502BadGateway);
}
