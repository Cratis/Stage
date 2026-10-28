// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_with_a_file_included_handler : Specification
{
    const string Source = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Registers a project"
                projectId ProjectId identifier
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                handler
                  file Handlers/RegisterProject.cs
              event ProjectRegistered
                projectId ProjectId
        """;

    SemanticCompilation _compilation = null!;
    AuthoringMetadataInput.Catalog? _catalog;
    bool _accepted;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("projects"), "projects", "Projects.play", Source);
        var attachments = ImmutableDictionary<string, string>.Empty.Add("Handlers/RegisterProject.cs", "return new ProjectRegistered(command.ProjectId);");
        var result = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog, attachments));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        _compilation = result.Value!;
    }

    void Because()
    {
        var input = AuthoringMetadataInput.Create(_compilation);
        input.ShouldNotBeNull();
        _accepted = AuthoringMetadataInput.TryRead(input!, _compilation.Model, out _catalog);
    }

    [Fact] void should_admit_the_metadata_it_creates() => _accepted.ShouldBeTrue();
    [Fact] void should_preserve_the_command_description() => _catalog!.Entries.Values.ShouldContain(new AuthoringMetadataInput.Metadata("Registers a project", null));
}
