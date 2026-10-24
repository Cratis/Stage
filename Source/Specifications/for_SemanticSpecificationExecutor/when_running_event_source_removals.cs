// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_event_source_removals : Specification
{
    [Theory]
    [InlineData("", false, true)]
    [InlineData(" key sourceId", false, true)]
    [InlineData(" key sourceId", true, true)]
    [InlineData("", true, false)]
    public async Task should_check_the_authored_removal_key_instead_of_always_using_the_event_source(string removalKey, bool numericSource, bool admitted)
    {
        var source = $$"""
            module Storage
              feature State
                slice StateChange Remove
                  event Stored
                    value String
                  event Removed
                    sourceId String
                  command Store
                    id String identifier
                    value String
                    produces Stored
                      for id
                      value = value
                  command Remove
                    id String identifier
                    sourceId String
                    produces Removed
                      for id
                      sourceId = sourceId
                  specification Removing
                    given Stored
                      for "reference"
                      value = "old"
                    when append Removed
                      for "reference"
                      sourceId = "reference"
                    then no readmodel Current for "reference"
                slice StateView Current
                  readmodel Current
                    id String key
                    value String
                  projection Build => Current
                    from Stored
                      id = $eventSourceId
                      value = value
                    remove with Removed{{removalKey}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RemovalKeys"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("RemovalKeys", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value.Model;
        if (numericSource)
        {
            model = WithNumericEventSource(model);
        }
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        Assert.True(result.Outcome == (admitted ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Unsupported),
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        if (!admitted) result.Unsupported.Capability.ShouldEqual(StageExecutionCapability.Projection);
    }

    static ExecutableSemanticModel WithNumericEventSource(ExecutableSemanticModel model)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => !slice.Specifications.IsEmpty);
        var specification = slice.Specifications.Single();
        specification = specification with
        {
            WhenAppended = specification.WhenAppended with
            {
                EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), SemanticValue.Number(42))
            }
        };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? slice with
            {
                Specifications = [specification],
                Commands = [.. slice.Commands.Select(command => command.Name != "Remove" ? command : command with
                {
                    Properties = [.. command.Properties.Select(property => property.IsIdentifier ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) } : property)],
                    Destination = command.Destination is null ? null : command.Destination with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) }
                })]
            } : candidate)] }] }]
        });
    }
}
