// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider;

public class when_omitting_a_required_filter : given.a_list_query_runtime
{
    Exception? _error;

    void Establish() => CreateProvider();
    async Task Because() => _error = await Catch.Exception(async () => await Perform("FilteredProjects", QueryArguments.Empty));

    [Fact] void should_reject_instead_of_returning_unfiltered_rows() => _error.ShouldBeOfExactType<SemanticQueryRejected>();
}
