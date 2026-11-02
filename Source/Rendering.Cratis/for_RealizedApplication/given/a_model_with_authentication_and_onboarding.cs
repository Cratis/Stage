// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specs;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication.given;

/// <summary>
/// A lending library that signs people in with three identity providers and lets them sign up or be invited, planned as a whole application.
/// </summary>
public class a_model_with_authentication_and_onboarding : Specification
{
    internal const string Application = "Lending";

    internal static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    protected string _folder = null!;
    protected LoadedSemanticModel _loaded = null!;
    protected ArtifactRenderPlan _plan = null!;

    protected string Text(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    protected bool Has(string path) => _plan.Artifacts.Any(artifact => artifact.RelativePath == path);

    protected static string Source(string onboarding = DefaultOnboarding) => $$"""
        concept BookId : Uuid
        concept BookTitle : String

        policy IsLibrarian
          require role "Librarian"

        policy IsMember
          require role "Member"

        policy IsSignedIn
          require authenticated

        persona Librarian
          description "Runs the catalog and invites members"
          policy IsLibrarian

        persona Member
          description "Borrows books"
          policy IsMember

        persona Visitor
          description "Anyone who is signed in"
          policy IsSignedIn

        authentication
          provider EntraId
          provider GitHub
          provider OpenId name Partner

        {{onboarding}}

        module Lending
          feature Catalog
            slice StateChange AddBook
              command AddBook
                bookId BookId identifier
                title  BookTitle
                authorize IsLibrarian

                produces BookAdded
                  for bookId
                  title = title

              event BookAdded
                title BookTitle
        """;

    protected const string DefaultOnboarding = """
        onboarding
          signup open creates tenant
          invite tenant as Librarian
          invite member
            by Librarian
            grant Member
          legal terms
            file Legal/terms-of-service.md
            file Legal/privacy-policy.md
          offering Starter
          emails
            invitation file Emails/invitation.html
            welcome file Emails/welcome.html
            signupDeclined file Emails/signup-declined.html
            images Emails/Images
        """;

    protected static void Write(string folder, string onboarding)
    {
        File.WriteAllText(Path.Combine(folder, "lending.play"), Source(onboarding));
        Directory.CreateDirectory(Path.Combine(folder, "Legal"));
        File.WriteAllText(Path.Combine(folder, "Legal", "terms-of-service.md"), "# Terms\n");
        File.WriteAllText(Path.Combine(folder, "Legal", "privacy-policy.md"), "# Privacy\n");
        Directory.CreateDirectory(Path.Combine(folder, "Emails", "Images"));
        File.WriteAllText(
            Path.Combine(folder, "Emails", "invitation.html"),
            "---\nsubject: Join {{application.name}}\n---\n<html><body><img src=\"cid:logo.png\" alt=\"Logo\"><a href=\"{{invitation.link}}\">Accept</a></body></html>\n");
        File.WriteAllText(Path.Combine(folder, "Emails", "welcome.html"), "<html><body><p>Welcome {{member.name}} to {{tenant.name}}</p></body></html>\n");
        File.WriteAllText(Path.Combine(folder, "Emails", "signup-declined.html"), "<html><body><p>Sorry, {{decline.message}}</p></body></html>\n");
        File.WriteAllBytes(Path.Combine(folder, "Emails", "Images", "logo.png"), Png);
    }

    protected virtual string Onboarding => DefaultOnboarding;

    async Task Establish()
    {
        _folder = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-realized-application")).FullName;
        Write(_folder, Onboarding);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, Application);
        var profile = CratisRendering.WithRealization(CratisRendering.CreateProfile(Application, new(Application, Application)), _loaded.Realization);
        var request = new ArtifactRenderRequest(_loaded.Model, _loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id))
        {
            SemanticIdentities = _loaded.SemanticIdentities,
            IdentityCatalog = _loaded.IdentityCatalog,
            ImplementationRequirements = _loaded.ImplementationRequirements,
            ImplementationContents = _loaded.ImplementationContents,
            TypedContextDescriptors = _loaded.TypedContextDescriptors
        };
        _plan = new CratisArtifactRenderPlanner().Plan(request);
    }

    void Destroy() => SpecTemporaryRoot.Delete(_folder);
}
