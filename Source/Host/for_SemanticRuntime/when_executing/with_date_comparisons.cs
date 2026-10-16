// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_date_comparisons : Specification
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public async Task should_match_every_reference_corpus_outcome(string formName)
    {
        var corpus = DateComparisonsCorpus.V26;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Revision.ShouldEqual(corpus.SemanticRevision);
        corpus.SpecificationExpectations.Length.ShouldEqual(6);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var fixture = plan.Specifications[expectation.Specification];
            SemanticRunAdmission.Check(plan, fixture).ShouldBeNull();
            var reference = new SemanticSpecificationRunner().Run(plan, fixture.Id);
            Assert.True(reference.Passed, string.Join(';', reference.Failures));
            var facts = new List<SemanticFact>();
            var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
            ((ISemanticFactTail)appender).Tail().Returns(_ => facts.Count == 0 ? ulong.MaxValue : (ulong)facts.Count - 1);
            appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
            var runtime = SemanticRuntimeHosting.Create(plan, appender);
            using var lifetime = (IDisposable)runtime;
            var command = plan.Commands[fixture.When!.Command];
            var values = fixture.When.Values.ToDictionary(value => command.Properties.Single(property => property.Id == value.TargetProperty).Name,
                value => JsonSerializer.SerializeToElement(((SemanticTextValue)value.Value).Value));
            var result = await runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), false);
            result.Kind.ShouldEqual(expectation.Outcome);
            result.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            facts.Count.ShouldEqual(expectation.WorldFactCount.Value);
            if (result is SemanticRejected rejected)
            {
                rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                rejected.Details.ShouldEqual(expectation.RejectionMessage);
                await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
            }
            else
            {
                ((SemanticAccepted)result).Facts.Length.ShouldEqual(((SemanticAccepted)reference.Execution).Facts.Length);
            }
        }
    }
}
