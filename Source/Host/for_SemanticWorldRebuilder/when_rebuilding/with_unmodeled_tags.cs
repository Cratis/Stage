// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_unmodeled_tags : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        _event.Context.Tags = ["not-produced"];
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));
    }

    [Fact] void should_refuse_the_unmodeled_tags() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_mismatched_metadata() => _error!.Message.ShouldContain("tags or occurrence");
}
