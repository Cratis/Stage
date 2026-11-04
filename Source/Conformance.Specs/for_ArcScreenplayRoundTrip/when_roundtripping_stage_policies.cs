// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_roundtripping_stage_policies : Specification
{
    const string Source = """
        policy Accountant
          require authenticated and role "Accountant"
        policy Automation
          require role "Automation"
        module Banking
          feature Accounting
            slice StateChange Booking
              command BookEntry
                id Uuid identifier
                name String
                authorize Accountant
                produces event EntryBooked
                  name String = name
            slice StateChange Jobs
              command RunJob
                id Uuid identifier
                name String
                authorize Automation
                produces event JobRun
                  name String = name
        """;

    [Theory]
    [InlineData("BookEntry", "Accountant", true)]
    [InlineData("RunJob", "Automation", false)]
    public void should_preserve_each_rendered_policy_condition(string command, string role, bool authenticated)
    {
        var recovered = given.arc_recovery.Render(Source);
        recovered.AssertHealthy();
        when_recovering_native_policies.AssertPolicy(recovered.Model, command, role, authenticated);
        Assert.DoesNotContain("SP0026", recovered.Result.Diagnostics.Select(diagnostic => diagnostic.Code));
    }
}
#endif
