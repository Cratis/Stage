// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_duplicated_stored_property : a_stored_partial_event
{
    Exception? _error;

    void Establish()
    {
        Prepare("String?");
        _stored.Content = """{"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","value":"first","value":"second"}""";
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_partialPlan, _stored));

    [Fact] void should_refuse_the_ambiguous_event() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
