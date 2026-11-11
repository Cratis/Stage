// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule.given;

public class an_invoice_command_with_an_unsupported_validation_rule : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected SemanticCommand _command = null!;
    SemanticExecutionPlan _executionPlan = null!;
    protected ArtifactRenderPlan _plan = null!;

    static string Source => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
        .Replace("        produces InvoiceIssued\n", "        validate\n          description not empty\n        produces InvoiceIssued\n", StringComparison.Ordinal);

    void Establish()
    {
        (_model, _executionPlan) = CreateModel(Unsupported);
        _command = _model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    protected virtual SemanticValidationRule Unsupported(SemanticValidationRule rule) => rule with { Kind = (SemanticValidationRuleKind)999 };

    protected ArtifactRenderPlan Plan(bool strict = false) => PlanModel((_model, _executionPlan), strict);

    public static (ExecutableSemanticModel Model, SemanticExecutionPlan Plan) CreateModel(Func<SemanticValidationRule, SemanticValidationRule>? unsupported = null)
    {
        var model = invoice_model.Compile(Source);
        var executionPlan = SemanticExecutionPlan.Compile(model).Plan ?? throw new InvalidOperationException("The invoice execution plan should compile.");
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var rule = command.Validations.Single();
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Validations = [unsupported is null ? rule with { Kind = (SemanticValidationRuleKind)999 } : unsupported(rule)] }] }] }] }]
        };

        // Bypass Screenplay's constructor validation to model an unaudited dependency enum value.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, application);
        return (model, executionPlan);
    }

    public static ArtifactRenderPlan PlanModel((ExecutableSemanticModel Model, SemanticExecutionPlan Plan) source, bool strict = false)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", Source);
        var compilation = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        return new CratisArtifactRenderPlanner().Plan(new(
            source.Model,
            source.Plan,
            CratisRendering.CreateProfile(source.Model.Application.Name, new("InvoiceApp", "Invoices") { Strict = strict }),
            new(ArtifactRenderScopeKind.Application, source.Model.Application.Id))
        {
            IdentityCatalog = compilation.Value.Documents.IdentityCatalog,
            SemanticIdentities = compilation.Value.Documents.IdentityCatalog.Semantics
        });
    }
}
