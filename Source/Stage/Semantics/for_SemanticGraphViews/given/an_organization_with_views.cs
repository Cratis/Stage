// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.given;

/// <summary>
/// An Organization graph read with collecting graph queries: an outgoing collect with edge properties, an incoming one, an optional
/// collect over a self edge and a collect along an identified edge, with conditions on the root and on a collected node.
/// </summary>
public class an_organization_with_views : Specification
{
    const string Source = """
        concept PersonId     : Uuid
        concept GroupId      : Uuid
        concept AssignmentId : Uuid

        type GroupMembership
          id    GroupId
          name  String optional
          since DateTime optional

        type GroupSummary
          id   GroupId
          name String optional

        type MemberSummary
          id    PersonId
          name  String optional
          since DateTime optional

        type RoleAssignment
          id           GroupId
          assignmentId AssignmentId
          role         String optional

        module Organization
          feature Chart
            slice StateView GraphViews
              graph Organization
                node Person
                  personId PersonId key
                  name     String
                node Group
                  groupId GroupId key
                  name    String
                edge MEMBER_OF source Person target Group
                  since DateTime
                edge PART_OF source Group target Group
                edge HAS_ROLE source Person target Group identified by assignmentId
                  assignmentId AssignmentId
                  role         String
                  since        DateTime

              readmodel PersonWithGroups
                id     PersonId key
                name   String optional
                groups GroupMembership[]

              readmodel GroupWithMembers
                id      GroupId key
                name    String optional
                members MemberSummary[]

              readmodel GroupWithParent
                id     GroupId key
                name   String optional
                parent GroupSummary optional

              readmodel PersonRoles
                id    PersonId key
                roles RoleAssignment[]

              query PeopleInGroup => PersonWithGroups[] graph Organization
                filter groupId GroupId
                filter name String
                match person:Person
                where group.groupId = $groupId and person.name = $name and group.name = "Engineering"
                return
                  id     = person.personId
                  name   = person.name
                  groups = collect person -membership:MEMBER_OF-> group:Group
                    id    = group.groupId
                    name  = group.name
                    since = membership.since

              query PersonById => PersonWithGroups optional graph Organization
                by personId PersonId
                match person:Person
                where person.personId = $personId
                return
                  id     = person.personId
                  name   = person.name
                  groups = collect person -membership:MEMBER_OF-> group:Group
                    id    = group.groupId
                    name  = group.name
                    since = membership.since

              query GroupsWithMembers => GroupWithMembers[] graph Organization
                match group:Group
                return
                  id      = group.groupId
                  name    = group.name
                  members = collect group <-membership:MEMBER_OF- member:Person
                    id    = member.personId
                    name  = member.name
                    since = membership.since

              query GroupsWithParent => GroupWithParent[] graph Organization
                match group:Group
                return
                  id     = group.groupId
                  name   = group.name
                  parent = collect group -PART_OF-> parent:Group
                    id   = parent.groupId
                    name = parent.name

              query PeopleWithRoles => PersonRoles[] graph Organization
                match person:Person
                return
                  id    = person.personId
                  roles = collect person -assignment:HAS_ROLE-> group:Group
                    id           = group.groupId
                    assignmentId = assignment.assignmentId
                    role         = assignment.role
        """;

    protected ExecutableSemanticModel _model = null!;
    protected SemanticApplication _application = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _model = compilation.Value.Model;
        _application = _model.Application;
    }

    protected SemanticGraphQuery Query(string name) => _application.GraphQueries.Single(query => query.Name == name);

    protected SemanticReadModel ReadModelOf(SemanticGraphQuery query) =>
        _application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.ReadModels).Single(readModel => readModel.Id == query.ReadModel);

    protected GraphQuery Lower(string name, params (string Name, object? Value)[] arguments)
    {
        var query = Query(name);
        return SemanticGraphViews.Lower(_application, query, ReadModelOf(query), arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal));
    }
}
