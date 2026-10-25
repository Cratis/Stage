// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_commands_with_a_competing_identity : Specification
{
    readonly ICollection<string> _commands = [];
    readonly ICollection<Type> _competingIdentityTypes = [];
    readonly List<string> _errors = [];
    readonly List<string> _warnings = [];
    readonly ICollection<(EventSourceId Actual, EventSourceId Expected)> _destinations = [];

    void Because()
    {
        foreach (var conceptDestination in new[] { false, true })
        {
            var plan = invoice_model.Plan(invoice_model.Compile(invoice_model.WithCompetingIdentity(conceptDestination)));
            plan.Success.ShouldBeTrue();
            var sources = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
                .Select(_ => new RenderedFile(_.RelativePath, Encoding.UTF8.GetString(_.Bytes.AsSpan()))).ToArray();
            _errors.AddRange(RenderedOutput.Errors(sources));
            _warnings.AddRange(RenderedOutput.Warnings(sources));
            _commands.Add(sources.Single(_ => _.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal)).Content);
            var assembly = RenderedOutput.Load(sources);
            var commandType = assembly.GetTypes().Single(_ => _.Name == "IssueInvoice");
            var competingType = assembly.GetTypes().Single(_ => _.Name == "OtherId");
            _competingIdentityTypes.Add(competingType);
            var competingId = Activator.CreateInstance(competingType, Guid.Parse("5fa85f64-5717-4562-b3fc-2c963f66afa8"))!;
            foreach (var value in new[] { "3fa85f64-5717-4562-b3fc-2c963f66afa6", "4fa85f64-5717-4562-b3fc-2c963f66afa7" })
            {
                var destination = conceptDestination
                    ? Activator.CreateInstance(assembly.GetTypes().Single(_ => _.Name == "InvoiceStream"), Guid.Parse(value))!
                    : value;
                var command = (ICanProvideEventSourceId)Activator.CreateInstance(commandType, competingId, "Payload", destination)!;
                _destinations.Add((command.GetEventSourceId(), new EventSourceId(value)));
            }
        }
    }

    [Fact] void should_keep_the_competing_concept_as_a_typed_event_source_identity() => _competingIdentityTypes.All(_ => typeof(EventSourceId<Guid>).IsAssignableFrom(_)).ShouldBeTrue();
    [Fact] void should_keep_the_earlier_typed_identity() => _commands.All(_ => _.Contains("IssueInvoice(OtherId OtherId, string Description,", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explicitly_provide_the_nonconventional_destination() => _commands.All(_ => _.Contains("public EventSourceId GetEventSourceId() => StreamReference;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_depend_on_key_property_precedence() => _commands.All(_ => !_.Contains("[property: Key]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_convert_the_destination_instead_of_the_earlier_identity() => _destinations.All(_ => _.Actual == _.Expected).ShouldBeTrue();
    [Fact] void should_import_common_for_the_command_signature() => _commands.All(_ => _.Contains("using Invoices.Common;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_both_forms_and_their_debug_specifications() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, _warnings).ShouldEqual(string.Empty);
}
