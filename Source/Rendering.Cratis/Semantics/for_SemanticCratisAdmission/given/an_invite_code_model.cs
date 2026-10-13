// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;

public static class an_invite_code_model
{
    public const string Source = """
        concept InviteCode : String
          generate nanoid 6 alphabet "AB"
        concept MixedInviteCode : String
          generate nanoid 6 alphabet "ABab"
        module Shop
          feature Invitations
            slice StateChange Invite
              constraint UniqueInviteCode
                unique code on Invited
                released by InviteReleased
                message "Invite code is already in use"
              event Invited
                code InviteCode
              event InviteReleased
              command InviteCustomer
                inviteId String identifier
                inviteCode InviteCode generated
                produces Invited
                  for inviteId
                  code = inviteCode
              specification Collision
                given Invited
                  for "A"
                  code = "AAAAAA"
                when InviteCustomer
                  inviteId = "B"
                  generated inviteCode = "AAAAAA"
                then error "Invite code is already in use"
              specification DistinctCode
                given Invited
                  for "A"
                  code = "AAAAAA"
                when InviteCustomer
                  inviteId = "B"
                  generated inviteCode = "BBBBBB"
                then Invited
                  for "B"
                  code = "BBBBBB"
              specification SameSourceReclaim
                given Invited
                  for "A"
                  code = "AAAAAA"
                when InviteCustomer
                  inviteId = "A"
                  generated inviteCode = "AAAAAA"
                then Invited
                  for "A"
                  code = "AAAAAA"
              specification ReleasedCode
                given Invited
                  for "A"
                  code = "AAAAAA"
                given InviteReleased
                  for "A"
                when InviteCustomer
                  inviteId = "B"
                  generated inviteCode = "AAAAAA"
                then Invited
                  for "B"
                  code = "AAAAAA"
            slice StateChange MixedInvite
              constraint UniqueMixedInviteCode
                unique code on MixedInvited
                ignore casing
              event MixedInvited
                code MixedInviteCode
              command InviteMixedCustomer
                inviteId String identifier
                inviteCode MixedInviteCode generated
                produces MixedInvited
                  for inviteId
                  code = inviteCode
              specification CaseInsensitiveCollision
                given MixedInvited
                  for "A"
                  code = "AaAaAa"
                when InviteMixedCustomer
                  inviteId = "B"
                  generated inviteCode = "aAaAaA"
                then error "Constraint 'UniqueMixedInviteCode' is violated: another event source already holds the constrained value."
        """;

    public static ExecutableSemanticModel Model() => invoice_model.Compile(Source);
}
