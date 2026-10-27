// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.for_KnownTypes;

public class when_capturing_known_types : Specification
{
    KnownTypes<string> _metadata;
    Exception _error = null!;

    void Because()
    {
        string[] values = ["String"];
        _metadata = KnownTypes<string>.Of(values);
        values[0] = "Changed";
        _error = Catch.Exception(() => ((IList<string>)_metadata.Values)[0] = "ChangedAgain");
    }

    [Fact] void should_be_known() => _metadata.IsKnown.ShouldBeTrue();
    [Fact] void should_snapshot_the_input() => _metadata.Values[0].ShouldEqual("String");
    [Fact] void should_not_expose_a_mutable_array() => _error.GetType().ShouldEqual(typeof(NotSupportedException));
}
