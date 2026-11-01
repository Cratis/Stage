// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_a_file_reaction_body : Specification
{
    const string Notify = """
        module Support
          feature Tickets
            slice Automation Notify
              event TicketOpened
                title String
              reaction Notifier
                when TicketOpened
        """;

    const string Exercising = """

              specification Notifying
                when append TicketOpened
                  title = "Broken"
                then no events
        """;

    string _workspace = null!;
    LoadedSemanticModel _file = null!;
    LoadedSemanticModel _inline = null!;
    SemanticUnsupportedCapability? _fileRefusal;
    SemanticUnsupportedCapability? _inlineRefusal;
    SemanticSpecificationRunRecord _executed = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Establish()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "stage-file-reaction-body", Guid.NewGuid().ToString("N"));
        _file = await Load("file", Notify + "\n          file Body.cs" + Exercising, "return null;");
        _inline = await Load("inline", Notify + "\n          ```csharp\n          return null;\n          ```" + Exercising, null);
    }

    async Task Because()
    {
        var specification = _file.Plan.Specifications.Values.Single();
        _fileRefusal = SemanticRunAdmission.Check(_file.Plan, specification);
        _inlineRefusal = SemanticRunAdmission.Check(_inline.Plan, _inline.Plan.Specifications.Values.Single());
        _reference = new SemanticSpecificationRunner().Run(_file.Plan, specification.Id);
        _executed = (await new SemanticSpecificationExecutor().Run(_file.Plan, new([specification.Id]), new())).Results.Single();
    }

    void Destroy() => Directory.Delete(_workspace, recursive: true);

    [Fact] void should_refuse_the_file_body() => _fileRefusal.ShouldNotBeNull();
    [Fact] void should_report_the_reaction_capability() => _fileRefusal!.Capability.ShouldEqual(StageExecutionCapability.Reaction);
    [Fact] void should_refuse_exactly_as_the_inline_form_does() => _fileRefusal!.Details.ShouldEqual(_inlineRefusal!.Details);
    [Fact] void should_name_the_opaque_body() => _fileRefusal!.Details.ShouldContain("STAGE-ESM-024: Reaction 'Notifier' has an opaque body for trigger kind '");
    [Fact] void should_be_unsupported_in_the_reference() => _reference.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_be_unsupported_not_passed_in_the_executor() => _executed.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);

    async Task<LoadedSemanticModel> Load(string name, string source, string? attachment)
    {
        var folder = Path.Combine(_workspace, name);
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "Reaction.play"), source);
        if (attachment is not null) await File.WriteAllTextAsync(Path.Combine(folder, "Body.cs"), attachment);
        return await SemanticModelLoader.LoadFromPathAsync(folder);
    }
}
