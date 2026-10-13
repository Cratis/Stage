// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_compiling_protected_identities
{
    [Theory]
    [InlineData("PersonalName")]
    [InlineData("AccessToken")]
    public void should_refuse_before_host_registration(string concept)
    {
        var source = given.a_compliance_store.Source.Replace("id String identifier", $"id {concept} identifier", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("records"), "records", "Records.play", source);
        var compilation = new SemanticModelCompiler().Compile("Records", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeFalse();
    }
}
