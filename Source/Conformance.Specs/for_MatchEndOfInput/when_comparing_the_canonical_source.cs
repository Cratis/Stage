// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Specifications;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_MatchEndOfInput;

public class when_comparing_the_canonical_source : Specification
{
    SemanticExecutionPlan _plan = null!;
    Assembly _rendered = null!;
    readonly List<(string Name, bool Reference, bool Stage, bool Rendered)> _outcomes = [];

    void Establish()
    {
        using var resource = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.MatchEndOfInput.source.play")!;
        using var reader = new StreamReader(resource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MatchEndOfInput"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "MatchEndOfInput.play", reader.ReadToEnd());
        var compilation = new SemanticModelCompiler().Compile("MatchEndOfInput", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var rendered = CratisRendering.Plan(_plan.Model, _plan, new(ArtifactRenderScopeKind.Application, _plan.Model.Application.Id), new("MatchEndOfInput", "MatchEndOfInput"));
        Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics));
        _rendered = RenderedOutput.Load(rendered.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Code.cs", StringComparison.Ordinal)).Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))));
    }

    async Task Because()
    {
        var concept = _rendered.GetTypes().Single(type => type.Name == "Code");
        var validator = (IValidator)Activator.CreateInstance(_rendered.GetTypes().Single(type => type.Name == "CodeValidator"))!;
        foreach (var specification in _plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
            var stage = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
            var value = ((SemanticTextValue)specification.When!.Values.Single(value => _plan.Commands[specification.When.Command].Properties.Single(property => property.Id == value.TargetProperty).Name == "code").Value).Value;
            var instance = Activator.CreateInstance(concept, value)!;
            var contextType = typeof(ValidationContext<>).MakeGenericType(concept);
            var context = (IValidationContext)Activator.CreateInstance(contextType, instance)!;
            var valid = (await validator.ValidateAsync(context)).IsValid;
            var accepted = reference.Execution.Kind == SemanticExecutionOutcomeKind.Accepted;
            var expected = specification.ThenErrors.IsEmpty;
            _outcomes.Add((specification.Name, reference.Passed && accepted == expected, stage.Outcome == SemanticSpecificationOutcome.Passed, valid == expected));
        }
    }

    [Fact] void should_cover_both_acceptance_and_trailing_newline_rejection() => _outcomes.Select(outcome => outcome.Name).ShouldContainOnly(["AcceptingTheWholeInput", "RejectingATrailingNewline"]);
    [Fact] void should_match_all_three_paths() => _outcomes.TrueForAll(outcome => outcome.Reference && outcome.Stage && outcome.Rendered).ShouldBeTrue();
}
