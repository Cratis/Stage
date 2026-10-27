// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.for_KnownTypes;

public class when_metadata_is_unknown : Specification
{
    KnownTypes<string> _metadata;
    Exception _error = null!;

    void Establish() => _metadata = default;
    void Because() => _error = Catch.Exception(() => _ = _metadata.Values);

    [Fact] void should_default_to_unknown() => _metadata.IsKnown.ShouldBeFalse();
    [Fact] void should_offer_explicit_unknown() => KnownTypes<string>.Unknown.IsKnown.ShouldBeFalse();
    [Fact] void should_refuse_to_enumerate_unknown_as_empty() => _error.GetType().ShouldEqual(typeof(InvalidOperationException));
}
