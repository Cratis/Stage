// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_divergent_mirror : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        _mirror = new Dictionary<Cratis.Screenplay.Semantics.SemanticId, IReadOnlyList<string>>
        {
            [_readModel.Id] = ["{\"id\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"__initialized\":true,\"projectId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"name\":\"Different\"}"]
        };
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], _mirror, 0));
    }

    [Fact] void should_refuse_the_divergent_mirror() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_mirror() => _error!.Message.ShouldContain("disagrees");
}
