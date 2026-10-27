// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.for_KnownTypes;

public class when_the_explicit_list_is_null : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => KnownTypes<string>.Of(null!));

    [Fact] void should_refuse_to_turn_null_into_known_none() => _error.GetType().ShouldEqual(typeof(ArgumentNullException));
}
