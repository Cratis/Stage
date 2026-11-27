// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

internal static class StandaloneLongMatchSource
{
    internal static string AppendTo(string source) => "command reads protected\n" + source + "\n" + $$"""
        concept LongMatchValue : String
          validate
            matches "^(a{2996}b)+$" message "Long match rejected"

        module MatchPermissions
          feature Tracking
            slice StateChange Toggling
              event MatchEnabled
                allowed Bool
              event MatchDisabled
              command EnableMatch
                id String identifier
                produces MatchEnabled
                  for id
                  allowed = true
              command DisableMatch
                id String identifier
                produces MatchDisabled
                  for id
            slice StateView Permissions
              readmodel MatchPermission
                id String key
                allowed Bool
              projection TrackMatchPermission => MatchPermission
                from MatchEnabled
                  id = $eventSourceId
                  allowed = allowed
                remove with MatchDisabled

        module LongMatch
          feature Checking
            slice StateChange Check
              command CheckLongMatch
                id String
                value LongMatchValue
                reads MatchPermission by id
                validate
                  require MatchPermission.allowed == true
                    message "Match is not enabled"
                returns id
              specification AcceptingALongMatch
                given MatchEnabled
                  for "one"
                  allowed = true
                when CheckLongMatch
                  id = "one"
                  value = "{{new string('a', 2996)}}b"
                then returns "one"
              specification RejectingALongMatch
                given MatchEnabled
                  for "one"
                  allowed = true
                when CheckLongMatch
                  id = "one"
                  value = "{{new string('a', 2995)}}b"
                then error "Long match rejected"
        """;
}
