// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_MongoFactAttribute;

public class when_using_a_derived_fact_attribute : Specification
{
    bool _established;

    void Establish() => _established = true;

    [Fact] void should_be_an_xunit_fact() => typeof(MongoFactAttribute).IsAssignableTo(typeof(FactAttribute)).ShouldBeTrue();

    [Fact]
    void should_keep_the_mongo_connection_requirement() => new MongoFactAttribute().Skip.ShouldEqual(
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION"))
            ? "STAGE_CHRONICLE_MONGO_CONNECTION is not configured."
            : null);

    [MongoFact] void should_run_the_specification_lifecycle() => _established.ShouldBeTrue();
}
