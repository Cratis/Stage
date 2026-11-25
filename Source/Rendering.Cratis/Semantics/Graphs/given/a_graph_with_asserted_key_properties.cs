// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using static Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given.an_organization_graph_model;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;

internal static class a_graph_with_asserted_key_properties
{
    internal static string Source(bool eventSourceEdgeKey = false) =>
        (eventSourceEdgeKey ? an_organization_graph_model.Source.Replace("assignmentId AssignmentId", "assignmentId PersonId", StringComparison.Ordinal) : an_organization_graph_model.Source) + "\n" + $$"""
              specification AssertingGraphKeys
                given graph Organization
                  has node Person "{{PersonId}}"
                    name = "Ada"
                  has node Group "{{GroupId}}"
                    name = "Engineering"
                  has edge HAS_ROLE from "{{PersonId}}" to "{{GroupId}}" identified by "{{AssignmentId}}"
                    role = "Lead"
                then graph Organization
                  has node Person "{{PersonId}}"
                    personId = "{{PersonId}}"
                    name = "Ada"
                  has node Group "{{GroupId}}"
                    groupId = "{{GroupId}}"
                    name = "Engineering"
                  has edge HAS_ROLE from "{{PersonId}}" to "{{GroupId}}" identified by "{{AssignmentId}}"
                    assignmentId = "{{AssignmentId}}"
                    role = "Lead"
              specification AssertingEventBuiltGraphKeys
                when append PersonRegistered
                  for "{{PersonId}}"
                  name = "Ada"
                then graph Organization
                  has node Person "{{PersonId}}"
                    personId = "{{PersonId}}"
                    name = "Ada"
        """;
}
#endif
