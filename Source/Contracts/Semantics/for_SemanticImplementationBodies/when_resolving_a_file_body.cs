// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies.given;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies;

public class when_resolving_a_file_body : a_compiled_document_set
{
    const string Content = "return context.Value > 0;";

    ImmutableDictionary<string, string> _bodies = null!;

    void Establish() => Compile(FileSource, ImmutableDictionary<string, string>.Empty.Add("Rules/Positive.cs", Content));

    void Because() => _bodies = SemanticImplementationBodies.Resolve(_documents, _requirements);

    [Fact] void should_resolve_the_attachment_contents_for_the_requirement() => _bodies[_requirements.Single().RequirementId].ShouldEqual(Content);
}
