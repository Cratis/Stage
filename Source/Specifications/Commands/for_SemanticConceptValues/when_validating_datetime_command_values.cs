// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticConceptValues;

public class when_validating_datetime_command_values : Specification
{
    const string Source = """
        policy Member
          require authenticated
        module Scheduling
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                startsAt DateTime
                produces Kept
                  for id
              event Kept
        """;

    [Theory]
    [InlineData("2031-03-10T10:00:00Z")]
    [InlineData("2031-03-10T10:00:00.000Z")]
    [InlineData("2031-03-10T10:00:00.1Z")]
    [InlineData("2031-03-10T10:00:00.1234567Z")]
    [InlineData("2031-03-10T10:00:00+02:00")]
    [InlineData("2031-03-10T10:00:00.5-05:00")]
    [InlineData("2031-03-10T10:00:00.0000000+00:00")]
    public void should_accept_iso_8601_values_with_an_offset(string value) => Failure(value).ShouldBeNull();

    [Theory]
    [InlineData("March 10, 2031")]
    [InlineData("10/03/2031 10:00")]
    [InlineData("2031-03-10 10:00:00Z")]
    [InlineData("2031-03-10T10:00:00.12345678Z")]
    [InlineData("not-a-date")]
    public void should_refuse_other_forms_naming_the_expected_format_without_echoing_the_value(string value)
    {
        var failure = Failure(value);
        failure.ShouldEqual("A command property 'startsAt' expects an ISO 8601 date and time such as 2031-03-10T10:00:00Z.");
        failure!.Contains(value).ShouldBeFalse();
    }

    static string? Failure(string value)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DateInputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", Source);
        var compilation = new SemanticModelCompiler().Compile("DateInputs", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        var command = plan.Commands.Values.Single();
        var inputs = new Dictionary<string, string> { ["id"] = "window", ["startsAt"] = value };
        ImmutableArray<SemanticPropertyValue> values = [.. command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(inputs[property.Name])))];

        return SemanticConceptValues.RequestFailure(plan, command, values);
    }
}
