// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticHost.given;

public static class a_refused_specification
{
    public static string Source => for_SemanticRuntime.given.specification_plan.Source() + """

              specification BrokenExpectation
                when RegisterProject
                  projectId = "second"
                  name = "Screenplay"
                then NoSuchEvent
                  for "second"
        """;
}
