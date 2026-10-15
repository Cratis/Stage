// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_checking_checksum_fixtures
{
    [Theory]
    [InlineData("luhn", "0", true)]
    [InlineData("luhn", "1", false)]
    [InlineData("mod11", "0", true)]
    [InlineData("mod11", "1", false)]
    [InlineData("mod97", "98", true)]
    [InlineData("mod97", "99", false)]
    public void should_admit_only_pins_satisfying_the_authored_checksum(string algorithm, string pin, bool valid)
    {
        var model = invoice_model.Compile($$"""
            concept Reference : String
              generate nanoid {{pin.Length}} alphabet "0198"
              validate checksum {{algorithm}}
            module Shop
              feature References
                slice StateChange Issue
                  command Issue
                    value Reference generated
                    returns value
                  specification PinnedChecksum
                    when Issue
                      generated value = "{{pin}}"
                    then returns "{{pin}}"
            """);
        var plan = invoice_model.Plan(model);
        plan.Success.ShouldEqual(valid);
        if (!valid)
        {
            plan.Artifacts.ShouldBeEmpty();
            plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("fails concept 'Reference' rules", StringComparison.Ordinal));
        }
    }
}
