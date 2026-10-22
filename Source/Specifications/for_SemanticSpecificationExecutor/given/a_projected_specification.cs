// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_projected_specification : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected SemanticSpecificationRunRecord _result = null!;

    protected static SemanticExecutionPlan Compile(string scenario, string policy = "require role \"Staff\"", bool commandScenario = false, bool validateDescription = false)
    {
        var source = $$"""
            policy Access
            {{Indent(policy, 2)}}
            module Billing
              feature Invoices
                slice StateChange Registration
                  command RegisterInvoice
                    invoiceId String identifier
                    description String
            {{(validateDescription ? "        validate\n          description not empty message \"Description is required\"" : string.Empty)}}
                    produces InvoiceRegistered
                      for invoiceId
                      description = description
                  event InvoiceRegistered
                    description String
            {{(commandScenario ? Indent(scenario, 6) : string.Empty)}}
                slice StateView Lookup
                  readmodel InvoiceView
                    invoiceId String
                    description String
                  query InvoiceById => InvoiceView optional
                    by invoiceId String
                    authorize Access
                  projection InvoiceViews => InvoiceView
                    from InvoiceRegistered
                      invoiceId = $eventSourceId
                      description = description
            {{(commandScenario ? string.Empty : Indent(scenario, 6))}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var admitted = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.NotNull(admitted.Plan);

        return admitted.Plan!;
    }

    protected static SemanticExecutionPlan ExpectRejection(SemanticExecutionPlan plan, bool denied = false)
    {
        var model = plan.Model;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with
                        {
                            GivenEvents = [], ThenEvents = [], ThenReadModels = [], ThenAbsentReadModels = [], ThenQueries = [],
                            ThenErrors = denied ? [] : [new("LoweringRefused", "Object reference not set to an instance of an object.")],
                            ThenDenied = denied
                        })]
                    })]
                })]
            })]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    protected async Task Run()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        _result = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}
