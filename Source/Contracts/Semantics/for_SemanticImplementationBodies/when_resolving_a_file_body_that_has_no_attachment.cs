// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies.given;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies;

public class when_resolving_a_file_body_that_has_no_attachment : a_compiled_document_set
{
    ImmutableDictionary<string, string> _bodies = null!;

    void Establish() => Compile(FileSource);

    void Because() => _bodies = SemanticImplementationBodies.Resolve(_documents, _requirements);

    [Fact] void should_leave_the_requirement_unresolved() => _bodies.ShouldBeEmpty();
}
