// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_requiring_constraint_generation_fixtures : Specification
{
    const string SpecSource = """
              specification Attempt
                given Invited
                  for "A"
                  code = "AAAAAA"
                when InviteCustomer
                  inviteId = "B"
                then Invited
                  for "B"
                  code = "BBBBBB"
        """;
    static readonly string Declarations = an_invite_code_model.Source[..an_invite_code_model.Source.IndexOf("      specification Collision", StringComparison.Ordinal)];
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(Declarations + SpecSource);

    [Fact] void should_refuse_a_random_outcome_against_an_existing_claim() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-038" && diagnostic.Message == "Specification 'Attempt' needs a deterministic generated fixture for 'inviteCode' under constraint 'UniqueInviteCode'; an unpinned draw could change the constraint outcome.").ShouldBeTrue();
    [Fact] void should_not_emit_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_allow_live_generation_without_a_specification() => Plan(Declarations).Success.ShouldBeTrue();
    [Fact] void should_allow_unpinned_relational_expectations_without_prior_claims() => Plan(Declarations + SpecSource.Replace("        given Invited\n          for \"A\"\n          code = \"AAAAAA\"\n", string.Empty, StringComparison.Ordinal)).Success.ShouldBeTrue();
    [Fact] void should_allow_unpinned_generation_after_the_claim_is_released() => Plan(Declarations + SpecSource.Replace("        when InviteCustomer", "        given InviteReleased\n          for \"A\"\n        when InviteCustomer", StringComparison.Ordinal)).Success.ShouldBeTrue();
    [Fact] void should_allow_unpinned_reclaim_by_the_owner() => Plan(Declarations + SpecSource.Replace("inviteId = \"B\"", "inviteId = \"A\"", StringComparison.Ordinal).Replace("then Invited\n          for \"B\"", "then Invited\n          for \"A\"", StringComparison.Ordinal)).Success.ShouldBeTrue();
    [Fact] void should_keep_uuid_backed_constraint_payloads_refused() => Plan(Declarations.Replace("InviteCode : String\n  generate nanoid 6 alphabet \"AB\"", "InviteCode : Uuid\n  generate uuid", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-014").ShouldBeTrue();
    [Fact] void should_keep_multiple_claim_or_release_batches_refused() => Plan(Declarations.Replace("code = inviteCode", "code = inviteCode\n        produces InviteReleased\n          for inviteId", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-014").ShouldBeTrue();
    [Fact] void should_keep_ordinary_constrained_input_validation() => Plan(Declarations.Replace("inviteCode InviteCode generated", "inviteCode InviteCode", StringComparison.Ordinal)).Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Any(text => text.Contains("RuleFor(_ => _.InviteCode).NotNull()", StringComparison.Ordinal)).ShouldBeTrue();

    static ArtifactRenderPlan Plan(string source)
    {
        var model = invoice_model.Compile(source);
        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Invites", "Invites"));
    }
}
