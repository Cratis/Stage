// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_model_with_authentication_and_onboarding : Specification
{
    string _root = null!;
    LoadedSemanticModel _loaded = null!;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-realization")).FullName;
        Directory.CreateDirectory(Path.Combine(_root, "Emails", "Images"));
        File.WriteAllText(Path.Combine(_root, "Emails", "invitation.html"), "<p>{{invitation.link}}</p>");
        File.WriteAllBytes(Path.Combine(_root, "Emails", "Images", "logo.png"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_root, "Emails", "Images", "notes.txt"), [4]);
        File.WriteAllText(Path.Combine(_root, "shop.play"), """
            policy IsOwner
              require role "Owner"

            policy IsStaff
              require role "Staff"

            persona Owner
              policy IsOwner

            persona Staff
              policy IsStaff

            authentication
              provider GitHub
              provider OpenId name Partner

            onboarding
              signup open joins tenant
              invite member
                by Owner
                grant Staff
              emails
                invitation file Emails/invitation.html
                images Emails/Images

            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId Uuid identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
            """);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_root, null, "Shop");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_root);

    [Fact] void should_carry_the_providers_with_their_kind_and_identity() => string.Join(",", _loaded.Realization.Providers.Select(provider => $"{provider.Kind}:{provider.Identity}")).ShouldEqual("GitHub:GitHub,OpenId:Partner");
    [Fact] void should_carry_every_persona_with_the_roles_that_satisfy_its_policies() => string.Join(",", _loaded.Realization.Personas.Select(persona => $"{persona.Name}:{string.Join('+', persona.Roles)}")).ShouldEqual("Owner:Owner,Staff:Staff");
    [Fact] void should_mark_a_persona_it_could_reduce_to_roles_as_mappable() => _loaded.Realization.Personas.All(persona => persona.Mappable).ShouldBeTrue();
    [Fact] void should_carry_what_the_model_says_about_sign_up() => _loaded.Realization.Onboarding!.Signup.ShouldEqual("JoinsTenant");
    [Fact] void should_carry_who_may_invite_and_what_they_may_grant() => $"{string.Join('+', _loaded.Realization.Onboarding!.Invitations.Single().By)}>{string.Join('+', _loaded.Realization.Onboarding!.Invitations.Single().Grant)}".ShouldEqual("Owner>Staff");
    [Fact] void should_carry_the_email_template_files() => _loaded.Realization.Texts["Emails/invitation.html"].ShouldEqual("<p>{{invitation.link}}</p>");
    [Fact] void should_carry_the_images_of_the_templates() => _loaded.Realization.Images["logo.png"].ToArray().ShouldEqual(new byte[] { 1, 2, 3 });
    [Fact] void should_leave_out_a_file_that_is_not_an_image() => _loaded.Realization.Images.ContainsKey("notes.txt").ShouldBeFalse();
    [Fact] void should_list_the_environment_keys_the_model_needs() => _loaded.Realization.Environment.Select(key => key.Key).ShouldContain("onboarding.email.server.password");
    [Fact] void should_mark_the_secrets() => _loaded.Realization.Environment.Single(key => key.Key == "authentication.providers.GitHub.clientSecret").Secret.ShouldBeTrue();
    [Fact] void should_not_be_empty() => _loaded.Realization.IsEmpty.ShouldBeFalse();
}

public class when_loading_a_model_without_authentication : Specification
{
    LoadedSemanticModel _loaded = null!;
    string _root = null!;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-realization")).FullName;
        File.WriteAllText(Path.Combine(_root, "shop.play"), """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId Uuid identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
            """);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_root, null, "Shop");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_root);

    [Fact] void should_have_nothing_to_realize() => _loaded.Realization.IsEmpty.ShouldBeTrue();
}
#endif
