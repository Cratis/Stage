// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;

internal static class graph_projection_sources
{
    internal const string Split = """
        concept PersonId : Uuid

        module Organization
          feature Events
            slice StateChange Registrations
              command RegisterPerson
                personId PersonId identifier
                name String
                produces PersonRegistered
                  for personId
                  name = name
              event PersonRegistered
                name String
              event PersonRemoved
              event GroupCreated
                name String
              event MemberAdded
                groupId Uuid
              event MemberRemoved
                groupId Uuid
          feature Chart
            feature Views
              slice StateView ProjectionHost
                graph Organization
                  node Person
                    id Uuid key
                    name String
                  node Group
                    id Uuid key
                    name String
                  node Unhandled
                    id Uuid key
                  edge MEMBER_OF source Person target Group
                    since DateTime
                projection PeopleIndex => graph Organization
                  node Person
                    from PersonRegistered key $eventSourceId
                      name = name
                    remove with PersonRemoved key $eventSourceId detach
                  node Group
                    from GroupCreated key $eventSourceId
                      name = name
                projection MembershipIndex => graph Organization
                  edge MEMBER_OF
                    from MemberAdded source $eventSourceId target groupId
                      since = $eventContext.occurred
                    remove with MemberRemoved source $eventSourceId target groupId
        """;

    internal static string Regrouped => Split.Replace(
        "        projection MembershipIndex => graph Organization\n", string.Empty, StringComparison.Ordinal);

    internal static string SeparateSlice => Split.Replace(
        "        projection MembershipIndex => graph Organization",
        "      slice StateView MembershipHost\n        projection MembershipIndex => graph Organization",
        StringComparison.Ordinal);
}
#endif
