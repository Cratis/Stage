// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_unreadable_aggregate_path : given.a_policy_plan
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => CratisRendering.CheckPublication(_plan, _ => throw new DestinationFileUnreadable()));

    [Fact] void should_fail_closed_with_the_reader_failure() => _error.ShouldBeOfExactType<DestinationFileUnreadable>();

    sealed class DestinationFileUnreadable() : Exception("The destination file could not be read.");
}
#endif
