// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

internal static class StandaloneLongMatchSource
{
    internal static string AppendTo(string source) => source + "\n" + $$"""
        concept LongMatchValue : String
          validate
            matches "^(a{2996}b)+$" message "Long match rejected"

        module LongMatch
          feature Checking
            slice StateChange Check
              command CheckLongMatch
                id String
                value LongMatchValue
                returns id
              specification AcceptingALongMatch
                when CheckLongMatch
                  id = "one"
                  value = "{{new string('a', 2996)}}b"
                then returns "one"
              specification RejectingALongMatch
                when CheckLongMatch
                  id = "one"
                  value = "{{new string('a', 2995)}}b"
                then error "Long match rejected"
        """;
}
