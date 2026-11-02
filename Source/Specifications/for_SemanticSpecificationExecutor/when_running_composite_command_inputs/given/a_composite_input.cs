// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs.given;

public class a_composite_input : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticCommand _command = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected SemanticValidationFailure[] _failures = [];

    protected void Prepare(string type, string value, string? error = null)
    {
        var source = $$"""
            concept Count : Int
              validate
                > 5 severity error message "Count minimum"
                != 2 severity information message "Count excluded"
            concept Label : String
              validate
                not empty message "Label required"
                matches "^X" severity warning message "Label pattern"
            concept AccountCode : String
              validate
                checksum luhn
            type CheckedDetails
              count Count optional
              code AccountCode optional
            type Details
              count Count optional
              label Label optional
            type Node
              count Count optional
              next Node optional
            type RequiredDetails
              count Count
            module Inputs
              feature Checking
                slice StateChange Check
                  command CheckInput
                    id String
                    value {{type}}
                    returns id
                  specification CheckingInput
                    when CheckInput
                      id = "one"
                      value = {{value}}
                    {{(error is null ? "then returns \"one\"" : $"then error \"{error}\"")}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeInputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("inputs"), "inputs", "Inputs.play", source);
        var compilation = new SemanticModelCompiler().Compile("CompositeInputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        SetPlan(compilation.Value.Model);
    }

    protected void SetPlan(ExecutableSemanticModel model)
    {
        _plan = SemanticExecutionPlan.Compile(model).Plan;
        _specification = _plan.Specifications.Values.Single();
        _command = _plan.Commands.Values.Single();
    }

    protected void ChangeInput(SemanticValue value)
    {
        var specification = _specification with
        {
            When = _specification.When with
            {
                Values = [.. _specification.When.Values.Select(member => _command.Properties.Single(property => property.Id == member.TargetProperty).Name == "value" ? member with { Value = value } : member)]
            }
        };
        ChangeBehavior(specification, _command);
    }

    protected void ChangeBehavior(SemanticSpecification specification, SemanticCommand command)
    {
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        SetPlan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command], Specifications = [specification] }] }] }]
        }));
    }

    protected void VerifyReferenceFailures() => _failures.ShouldEqualInOrder([.. ((SemanticRejected)_reference.Execution).ValidationFailures]);

    protected async Task Execute()
    {
        _reference = new SemanticSpecificationRunner().Run(_plan, _specification.Id);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new())).Results);
        _failures = [.. SemanticRuleEvaluation.Failures(_command, _specification, _plan.Model.Application)];
    }
}
