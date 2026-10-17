// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.for_ReadModelConverter.given;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ScreenplayEventModelVisitor.when_visiting;

public class and_a_projection_is_unsupported : a_slice_with_projections
{
    Exception? _error;
    Exception? _legacyError;

    void Because()
    {
        _legacyError = Catch.Exception(() => SliceConverter.Convert(_slice, _schema, new Dictionary<string, string>(), "Store.Browsing"));
        _error = Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(_application));
    }

    [Fact] void should_preserve_the_legacy_refusal() => _error.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_preserve_the_legacy_message() => _error!.Message.ShouldEqual(_legacyError!.Message);
    [Fact] void should_include_the_construct() => _error!.Message.ShouldContain("sequence 'audit'");
    [Fact] void should_include_the_owning_slice() => _error!.Message.ShouldContain("Store.Browsing.Listing");
}
