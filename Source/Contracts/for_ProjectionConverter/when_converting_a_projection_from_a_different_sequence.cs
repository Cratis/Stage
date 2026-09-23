// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter;

public class when_converting_a_projection_from_a_different_sequence : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ProjectionConverter.Convert(
        new ProjectionSyntax("Summary", "Summary", "outbox", AutoMapMode.Inherit, null, [], SourceLocation.Start)));

    [Fact] void should_reject_unmodeled_sequence_routing() => _error.ShouldBeOfExactType<UnsupportedProjectionConversion>();
}
