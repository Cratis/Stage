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

    void Because() => _error = Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(_application));

    [Fact] void should_preserve_the_legacy_refusal() => _error.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_include_the_diagnostic_code() => _error!.Message.ShouldContain("STAGE-PROJECTION-001");
    [Fact] void should_include_the_construct() => _error!.Message.ShouldContain("projection sequence 'audit'");
}
