// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;

/// <summary>
/// Compiles a small organization: people who belong to groups in a graph that two projections' worth of events build.
/// </summary>
internal static class an_organization_graph_model
{
    internal const string PersonId = "6fa459ea-ee8a-4ca4-894e-db77e160355e";
    internal const string GroupId = "0f8fad5b-d9cb-469f-a165-70867728950e";
    internal const string AssignmentId = "9c858901-8a57-4791-81fe-4c455b099bc9";

    internal const string Source = """
        concept PersonId     : Uuid
        concept GroupId      : Uuid
        concept AssignmentId : Uuid
        concept PersonName   : String
        concept GroupName    : String

        module Organization
          feature People
            slice StateChange RegisterPerson
              command RegisterPerson
                personId PersonId identifier
                name     PersonName
                produces PersonRegistered
                  for personId
                  name = name
              event PersonRegistered
                name PersonName
              event PersonRemoved
              specification RegisteringAPerson
                when RegisterPerson
                  personId = "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name     = "Ada Lovelace"
                then PersonRegistered
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name = "Ada Lovelace"
            slice StateChange CreateGroup
              command CreateGroup
                groupId GroupId identifier
                name    GroupName
                produces GroupCreated
                  for groupId
                  name = name
              event GroupCreated
                name GroupName
              event MemberAdded
                groupId GroupId
              event MemberRemoved
                groupId GroupId
              event RoleAssigned
                groupId      GroupId
                assignmentId AssignmentId
                role         String
              event RoleRevoked
                groupId      GroupId
                assignmentId AssignmentId
              specification CreatingAGroup
                when CreateGroup
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name    = "Engineering"
                then GroupCreated
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "Engineering"

          feature Chart
            slice StateView OrganizationChart
              graph Organization
                node Person
                  personId PersonId key
                  name     PersonName
                node Group
                  groupId GroupId key
                  name    GroupName
                edge MEMBER_OF source Person target Group
                  since DateTime
                edge HAS_ROLE source Person target Group identified by assignmentId
                  assignmentId AssignmentId
                  role         String

              projection OrganizationChart => graph Organization
                node Person
                  from PersonRegistered key $eventSourceId
                    personId = $eventSourceId
                    name     = name
                  remove with PersonRemoved key $eventSourceId detach
                node Group
                  from GroupCreated key $eventSourceId
                    groupId = $eventSourceId
                    name    = name
                edge MEMBER_OF
                  from MemberAdded source $eventSourceId target groupId
                    since = $eventContext.occurred
                  remove with MemberRemoved source $eventSourceId target groupId
                edge HAS_ROLE
                  from RoleAssigned source $eventSourceId target groupId identified by assignmentId
                    assignmentId = assignmentId
                    role         = role
                  remove with RoleRevoked source $eventSourceId target groupId identified by assignmentId

              specification AddingAMemberDrawsAnEdge
                given PersonRegistered
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name = "Ada Lovelace"
                given GroupCreated
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "Engineering"
                when append MemberAdded
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                then graph Organization
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                    name = "Engineering"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"

              specification RevokingARoleDeletesOnlyThatEdge
                given RoleAssigned
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId      = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  assignmentId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  role         = "Lead"
                when append RoleRevoked
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId      = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  assignmentId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then graph Organization
                  no edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9c858901-8a57-4791-81fe-4c455b099bc9"

              specification RemovingAPersonDetachesTheirEdges
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada Lovelace"
                when append PersonRemoved
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                then graph Organization
                  no node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
        """;

    internal static ArtifactRenderRequest Request(string? source = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Organization"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("organization"), "organization", "Organization.play", source ?? Source);
        var compilation = new SemanticModelCompiler().Compile("Organization", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var model = compilation.Value.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var options = new CratisRenderingOptions("Organization", "Organization");
        var profile = CratisRendering.CreateProfile(model.Application.Name, options);
        return new(model, execution.Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    internal static SemanticApplicationContext Context(string? source = null) => new(Request(source), new("Organization", "Organization"));
}
#endif
