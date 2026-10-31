// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_csharp_conventions : Specification
{
    const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        policy Access
          require authenticated
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                authorize Access
                validate
                  name not empty message "Project name is required"
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
              specification RegisteringAProject
                given caller
                  authenticated
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
        """;

    ArtifactRenderPlan _plan = null!;
    string _command = null!;
    string _specification = null!;
    IReadOnlyList<string> _errors = null!;
    IReadOnlyList<string> _unusedSpecificationImports = null!;

    void Because()
    {
        _plan = when_rendering_authoring_documentation.Plan(when_rendering_authoring_documentation.Compile(Source));
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        _command = Text("RegisterProject.cs");
        _specification = Text("when_registering_a_project.cs");
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        var diagnostics = RenderedOutput.CreateCompilation(files).GetDiagnostics();
        _errors = [.. diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString())];
        _unusedSpecificationImports = [.. diagnostics.Where(diagnostic => diagnostic.Id == "CS8019" && diagnostic.Location.SourceTree.FilePath.EndsWith("when_registering_a_project.cs", StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.ToString())];
    }

    [Fact] void should_document_the_command_without_authored_metadata() => _command.ShouldContain("/// Represents the RegisterProject command.");
    [Fact] void should_document_the_handler() => _command.ShouldContain("/// Handles the RegisterProject command.");
    [Fact] void should_document_the_validator_constructor() => _command.ShouldContain("/// Initializes validation rules for RegisterProject.");
    [Fact] void should_describe_the_event_as_a_fact() => _command.ShouldContain("/// Records that project registered.");
    [Fact] void should_document_the_command_property() => _command.ShouldContain("/// <param name=\"ProjectId\">The project id.</param>");
    [Fact] void should_document_the_handler_result() => _command.ShouldContain("/// <returns>The modeled command outcome.</returns>");
    [Fact] void should_use_the_claims_namespace() => _specification.ShouldContain("using System.Security.Claims;");
    [Fact] void should_not_qualify_claims_types() => _specification.ShouldNotContain("global::System.Security.Claims.");
    [Fact] void should_import_command_assertion_extensions() => _specification.ShouldContain("using Cratis.Arc.Testing.Commands;");
    [Fact] void should_use_the_scaffold_result_namespace() => _specification.ShouldNotContain("using Cratis.Arc.Commands;");
    [Fact] void should_import_the_fact_namespace_for_shortened_attributes() => _specification.ShouldContain("using Xunit;");
    [Fact] void should_document_policy_value_matching() => Text("Policies.cs").ShouldContain("/// Matches a claim against the target using ordinal value comparison.");

    [Fact] void should_compile_the_generated_members_and_specification() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_have_no_unused_specification_imports() => _unusedSpecificationImports.ShouldBeEmpty();
    string Text(string name) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
