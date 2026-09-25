// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Specifications;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

// The generated application is built and tested once per semantic model, not once per specification or source form.
public abstract class a_three_way_application : a_generated_application
{
    protected abstract string ApplicationName { get; }
    protected abstract string ProjectFile { get; }
    protected abstract IEnumerable<(string Form, ExecutableSemanticModel Model)> Models { get; }
    protected virtual CanonicalCorpusVector? Baseline => null;
    protected virtual bool ExpectFailedGeneratedTest => false;
    protected virtual bool RequireStageExecution => false;
    protected override bool AllowBlockedPlan => true;

    public ImmutableArray<string> Outcomes { get; private set; }
    public ImmutableArray<string> Differences { get; private set; }

    protected override ArtifactRenderPlan CreatePlan()
    {
        var models = Models.ToArray();
        var first = models[0].Model;
        return CratisRendering.Plan(first, SemanticExecutionPlan.Compile(first).Plan!, new(ArtifactRenderScopeKind.Application, first.Application.Id), new(ProjectFile[..^7], ApplicationName));
    }

    protected async Task Verify()
    {
        var differences = new List<string>();
        var outcomes = new List<string>();
        try
        {
            var models = Models.ToArray();
            var rendered = CreatePlan();
            if (rendered.Success)
            {
                var warnings = BuildWarnings(await Run("parity-debug-build.log", "build", ProjectFile, "-c", "Debug", "-warnaserror", "--nologo"));
                Assert.True(warnings.Length == 0, warnings);
                await Run("parity-debug-test.log", ExpectFailedGeneratedTest, "test", ProjectFile, "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=parity.trx", "--results-directory", _evidence.FullName);
                var releaseWarnings = BuildWarnings(await Run("parity-release-build.log", "build", ProjectFile, "-c", "Release", "-warnaserror", "--nologo"));
                Assert.True(releaseWarnings.Length == 0, releaseWarnings);
            }
            else
            {
                Assert.Empty(rendered.Artifacts);
                Assert.NotEmpty(rendered.Diagnostics);
                Assert.All(rendered.Diagnostics, diagnostic => Assert.StartsWith("STAGE-ESM-", diagnostic.Code));
                differences.Add($"{ApplicationName}: the previously renderable conformance model was blocked: {string.Join(", ", rendered.Diagnostics.Select(diagnostic => diagnostic.Code))}");
            }

            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var results = rendered.Success
                ? XDocument.Load(Path.Combine(_evidence.FullName, "parity.trx"))
                    .Descendants(ns + "UnitTestResult")
                    .Select(result => (Name: result.Attribute("testName")!.Value, Outcome: result.Attribute("outcome")!.Value)).ToArray()
                : [];
            foreach (var (form, model) in models)
            {
                var plan = SemanticExecutionPlan.Compile(model).Plan!;
                if (Baseline is { } baseline && (plan.Specifications.Count != baseline.SpecificationExpectations.Length ||
                    baseline.SpecificationExpectations.Any(expectation => !plan.Specifications.ContainsKey(expectation.Specification))))
                {
                    differences.Add($"{form}: specification IDs differ from the frozen corpus");
                }

                var candidate = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new(ProjectFile[..^7], ApplicationName));
                var sameArtifacts = candidate.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256))
                    .SequenceEqual(rendered.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
                if (candidate.Success != rendered.Success || (candidate.Success && !sameArtifacts))
                {
                    differences.Add($"{form}: rendered output differs from the built model");
                }

                foreach (var specification in plan.Specifications.Values)
                {
                    var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                    if (Baseline is { } referenceBaseline)
                    {
                        var expectation = referenceBaseline.SpecificationExpectations.Single(value => value.Specification == specification.Id);
                        var wrongMessage = expectation.RejectionMessage is { } message && reference.Execution is SemanticRejected rejected && rejected.Details != message;
                        if (!reference.Passed || reference.Execution.Kind != expectation.Outcome || wrongMessage)
                        {
                            differences.Add($"{form}/{specification.Name}: reference differs from frozen corpus");
                        }
                    }

                    var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
                    var unexpectedCorpusBlock = Baseline is not null &&
                        (reference.Execution.Kind != SemanticExecutionOutcomeKind.Accepted || stage.Unsupported?.Capability != StageExecutionCapability.Projection);
                    if (stage.Outcome == SemanticSpecificationOutcome.Unsupported &&
                        (stage.Unsupported is null || RequireStageExecution || unexpectedCorpusBlock))
                    {
                        differences.Add($"{form}/{specification.Name}: unexpected Stage unsupported outcome {stage.Unsupported?.Capability}");
                    }

                    var testClass = ".when_" + Identifiers.ToSnakeCase(specification.Name) + ".";
                    var facts = results.Where(result => result.Name.Contains(testClass, StringComparison.Ordinal)).ToArray();
                    var rendering = "NotRendered";
                    if (facts.Length > 0)
                    {
                        rendering = facts.All(fact => fact.Outcome == "Passed") ? "Passed" : "Failed";
                    }
                    var line = $"{form}/{specification.Name}: Reference={(reference.Passed ? "Passed" : "Failed")}, Stage={stage.Outcome}" +
                        $"{(stage.Unsupported is null ? "" : $"({stage.Unsupported.Capability})")}, Rendered={rendering}";
                    outcomes.Add(line);
                    var stagePassed = stage.Outcome == SemanticSpecificationOutcome.Passed;
                    var stageExecuted = stage.Outcome is SemanticSpecificationOutcome.Passed or SemanticSpecificationOutcome.Failed;
                    if (stageExecuted && stagePassed != reference.Passed)
                    {
                        differences.Add(line);
                    }
                    if (rendered.Success && facts.Length == 0)
                    {
                        differences.Add($"{line}: generated specification missing from the TRX run");
                    }
                    if (facts.Length > 0 && rendering == "Passed" != reference.Passed)
                    {
                        differences.Add(line);
                    }
                    if (stage.Outcome is SemanticSpecificationOutcome.Cancelled)
                    {
                        differences.Add(line);
                    }
                }
            }
        }
        finally
        {
            Outcomes = [.. outcomes];
            Differences = [.. differences];
            Cleanup();
        }
    }

    protected static ExecutableSemanticModel Compile(CanonicalCorpusVector corpus, CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        Assert.Equal(corpus.SemanticRevision, compilation.Value!.Model.Revision);
        return compilation.Value.Model;
    }

    protected static ExecutableSemanticModel CompileWrongBilling()
    {
        var model = CompileBilling();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single(value => value.Name == "RegisteringAnInvoice");
        var fact = specification.ThenEvents.Single();
        var invoiceNumber = fact.Values.Single(value => value.Value is SemanticTextValue text && text.Value == "INV-000123");
        var changedFact = fact with { Values = [.. fact.Values.Select(value => value.TargetProperty == invoiceNumber.TargetProperty ? value with { Value = SemanticValue.Text("INV-000999") } : value)] };
        var changed = slice with { Specifications = [.. slice.Specifications.Select(value => value.Id == specification.Id ? value with { ThenEvents = [changedFact] } : value)] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
    }

    protected static ExecutableSemanticModel CompileBilling()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing-validation"), "billing-validation", "RegisterInvoice.play", when_running_inline_billing_validation.Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation.Value!.Model;
    }
}
#endif
