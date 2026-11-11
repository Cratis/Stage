// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

// Only the author may edit an article, run by both engines: the author is admitted, someone else is denied, and a caller that cannot say who it is
// is denied, because a policy that cannot tell who is asking must not allow.
public class when_running_policies_that_compare_with_the_callers_identifier : Specification
{
    const string Source = """
        concept ArticleSlug : String
        policy IsAuthor
          reads Article as article by slug
          require authenticated and article.author == $identity.id
        module Publishing
          feature Articles
            slice StateChange EditArticle
              command EditArticle
                slug ArticleSlug identifier
                authorize IsAuthor
                produces ArticleEdited
                  for slug
                  slug = slug
              event ArticleEdited
                slug ArticleSlug
              specification TheAuthorMayEdit
                given caller
                  authenticated
                  id "alice"
                given readmodel Article
                  slug = "a1"
                  author = "alice"
                when EditArticle
                  slug = "a1"
                then ArticleEdited
                  for "a1"
                  slug = "a1"
              specification SomeoneElseMayNot
                given caller
                  authenticated
                  id "bob"
                given readmodel Article
                  slug = "a1"
                  author = "alice"
                when EditArticle
                  slug = "a1"
                then denied
              specification ACallerWithNoIdentifierMayNot
                given caller
                  authenticated
                given readmodel Article
                  slug = "a1"
                  author = "alice"
                when EditArticle
                  slug = "a1"
                then denied
            slice StateView Articles
              readmodel Article
                slug ArticleSlug key
                author String
              event ArticleWritten
                author String
              projection BuildArticle => Article
                from ArticleWritten
                  slug = $eventSourceId
                  author = author
        """;

    readonly List<string> _differences = [];
    int _executed;

    [Fact]
    public void should_run_every_specification() => _executed.ShouldEqual(3);

    [Fact]
    public void should_match_the_reference_for_every_plan() => Assert.True(_differences.Count == 0, string.Join(Environment.NewLine, _differences));

    protected async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Publishing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("articles"), "articles", "Articles.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Publishing", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        foreach (var specification in plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            _executed++;
            if (!reference.Passed || result.Outcome != SemanticSpecificationOutcome.Passed)
            {
                _differences.Add($"{specification.Name}: reference {reference.Execution.Kind}/{reference.Passed} {string.Join(';', reference.Failures)}; Stage {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            }
        }
    }
}
