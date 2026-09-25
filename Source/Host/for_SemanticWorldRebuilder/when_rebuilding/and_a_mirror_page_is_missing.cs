// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class and_a_mirror_page_is_missing : a_registered_mirror
{
    Exception? _error;
    int _lastPage;

    void Establish() => _services.ReadModels.GetInstances(Arg.Any<GetInstancesRequest>()).Returns(call =>
    {
        _lastPage = call.Arg<GetInstancesRequest>().Page;
        return new GetInstancesResponse { TotalCount = 2, Instances = _lastPage == 0 ? [_mirror.Values.Single().Single()] : [] };
    });

    async Task Because() => _error = await Catch.Exception(() => SemanticChronicleRegistration.Rebuild(_accessor, "Projects", _plan, 0));

    [Fact] void should_request_the_next_page() => _lastPage.ShouldEqual(1);
    [Fact] void should_refuse_an_incomplete_mirror() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
