// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_failed_mirror_partition : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SemanticChronicleRegistration.EnsureNoFailures(
        [new Cratis.Chronicle.Contracts.Observation.FailedPartition { ObserverId = "mirror", IsResolved = false }],
        new HashSet<string> { "mirror" }));

    [Fact] void should_refuse_the_failed_partition() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_failed_partition() => _error!.Message.ShouldContain("failed partitions");
}
