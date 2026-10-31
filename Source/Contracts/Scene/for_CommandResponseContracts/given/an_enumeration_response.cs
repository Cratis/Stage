// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts.given;

public class an_enumeration_response : Specification
{
    protected SemanticApplication Application = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("responses"), "responses", "Responses.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        Application = compilation.Value!.Model.Application;
    }

    const string Source = """
        concept ProjectStatus : Enum
          draft
          active
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                status ProjectStatus
                returns status
            slice StateChange UpdateStatus
              command UpdateStatus
                status ProjectStatus
        """;
}
