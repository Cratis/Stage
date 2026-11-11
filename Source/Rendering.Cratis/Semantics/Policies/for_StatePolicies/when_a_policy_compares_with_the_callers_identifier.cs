// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_StatePolicies;

// "Only the author may edit it": the policy compares the recorded author with the identifier of the principal Arc authorized. A principal that carries no
// identifier, or two that disagree, makes the policy unavailable, so it denies; guessing a claim would let the wrong caller edit someone else's article.
public class when_a_policy_compares_with_the_callers_identifier : Specification
{
    const string Source = """
        concept ArticleSlug : String
        policy IsAuthor
          reads Article as article by slug
          require authenticated and article.author == $identity.id
        policy IsNotAuthor
          reads Article as article by slug
          require authenticated and article.author != $identity.id
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
            slice StateChange ReviewArticle
              command ReviewArticle
                slug ArticleSlug identifier
                authorize IsNotAuthor
                produces ArticleReviewed
                  for slug
                  slug = slug
              event ArticleReviewed
                slug ArticleSlug
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
    Assembly _assembly = null!;
    ExecutableSemanticModel _model = null!;

    bool _author;
    bool _someoneElse;
    bool _nameIdentifier;
    bool _subject;
    bool _noIdentifier;
    bool _disagreeing;
    bool _guest;
    bool _reviewerWhoIsNotTheAuthor;
    bool _reviewerWhoIsTheAuthor;
    bool _reviewerWithNoIdentifier;

    void Because()
    {
        var loaded = opaque_policy_model.Load(Source);
        _model = loaded.Model;
        _plan = opaque_policy_model.Plan(loaded);
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        _assembly = opaque_policy_model.Compile(_plan);

        _author = Edit(Authenticated(new Claim("urn:cratis:identity:subject", "alice")));
        _someoneElse = Edit(Authenticated(new Claim("urn:cratis:identity:subject", "bob")));
        _nameIdentifier = Edit(Authenticated(new Claim(ClaimTypes.NameIdentifier, "alice")));
        _subject = Edit(Authenticated(new Claim("sub", "alice")));
        _noIdentifier = Edit(Authenticated());
        _disagreeing = Edit(Authenticated(new Claim("urn:cratis:identity:subject", "alice"), new Claim("urn:cratis:identity:subject", "bob")));
        _guest = Edit(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "alice")])));
        _reviewerWhoIsNotTheAuthor = Review(Authenticated(new Claim(ClaimTypes.NameIdentifier, "bob")));
        _reviewerWhoIsTheAuthor = Review(Authenticated(new Claim(ClaimTypes.NameIdentifier, "alice")));
        _reviewerWithNoIdentifier = Review(Authenticated());
    }

    [Fact] void should_render_the_policy() => _plan.Success.ShouldBeTrue();
    [Fact] void should_allow_the_author() => _author.ShouldBeTrue();
    [Fact] void should_deny_someone_else() => _someoneElse.ShouldBeFalse();
    [Fact] void should_read_the_name_identifier_when_the_provider_subject_is_absent() => _nameIdentifier.ShouldBeTrue();
    [Fact] void should_read_the_sub_claim_last() => _subject.ShouldBeTrue();
    [Fact] void should_deny_a_principal_that_carries_no_identifier() => _noIdentifier.ShouldBeFalse();
    [Fact] void should_deny_a_principal_whose_identifiers_disagree() => _disagreeing.ShouldBeFalse();
    [Fact] void should_deny_a_caller_who_is_not_authenticated() => _guest.ShouldBeFalse();
    [Fact] void should_allow_a_reviewer_who_is_not_the_author() => _reviewerWhoIsNotTheAuthor.ShouldBeTrue();
    [Fact] void should_deny_the_author_reviewing_their_own_article() => _reviewerWhoIsTheAuthor.ShouldBeFalse();
    [Fact] void should_not_allow_a_reviewer_who_cannot_say_who_they_are() => _reviewerWithNoIdentifier.ShouldBeFalse();

    static ClaimsPrincipal Authenticated(params Claim[] claims) => new(new ClaimsIdentity(claims, "Stage"));

    bool Edit(ClaimsPrincipal principal) => Allows("EditArticle", principal);

    bool Review(ClaimsPrincipal principal) => Allows("ReviewArticle", principal);

    bool Allows(string commandName, ClaimsPrincipal principal)
    {
        var command = _model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single(candidate => candidate.Name == commandName);
        var type = _assembly.GetTypes().Single(candidate => candidate.Name == commandName);
        var slug = _assembly.GetTypes().Single(candidate => candidate.Name == "ArticleSlug");
        var article = _assembly.GetTypes().Single(candidate => candidate.Name == "Article");
        var constructor = type.GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(parameter => parameter.ParameterType == slug ? Activator.CreateInstance(slug, "first") : "Title").ToArray();
        var instance = constructor.Invoke(arguments);
        var stored = CreateArticle(article, slug);
        var readModels = when_rendering_a_policy_that_decides_from_recorded_state.ReadModelsProxy.Create(article, stored, unreadable: false);
        var policyType = _assembly.GetTypes().Single(candidate => candidate.Name == policy_names.ClassOf(_model, command.Id));
        var policy = (IAuthorizationPolicy)Activator.CreateInstance(policyType, readModels)!;
        var context = new AuthorizationPolicyContext(principal, type, new CommandContext(CorrelationId.New(), type, instance, [], CommandContextValues.Empty)) { ReceivedAt = opaque_policy_model.Receipt };

        return policy.IsAuthorized(context, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    static object CreateArticle(Type article, Type slug)
    {
        var constructor = article.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        var arguments = constructor.GetParameters().Select(parameter => parameter.ParameterType == slug ? Activator.CreateInstance(slug, "first") : "alice").ToArray();

        return constructor.Invoke(arguments);
    }
}
#endif
