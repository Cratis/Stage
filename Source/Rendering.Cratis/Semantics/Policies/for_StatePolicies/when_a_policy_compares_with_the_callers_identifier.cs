// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_StatePolicies;

// Stage has no mapping from Arc's principal to the caller's identifier yet (Cratis/Stage#1061). A policy that compares with it is refused,
// never rendered with a guessed claim, because a wrong guess would let the wrong caller edit someone else's article.
public class when_a_policy_compares_with_the_callers_identifier : Specification
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
                title String
                authorize IsAuthor
                produces ArticleEdited
                  for slug
                  slug = slug
                  title = title
              event ArticleEdited
                slug ArticleSlug
                title String
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

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(Source);

    [Fact] void should_not_render_the_policy() => _plan.Success.ShouldBeFalse();
    [Fact] void should_say_which_rule_refused_it() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldBeTrue();
}
#endif
