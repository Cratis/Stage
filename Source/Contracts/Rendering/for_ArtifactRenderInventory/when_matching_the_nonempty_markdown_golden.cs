// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_matching_the_nonempty_markdown_golden : given.an_inventory
{
    [Fact]
    void should_pin_complete_unicode_and_two_hop_report_bytes()
    {
        var expected = $$"""
            # Unsupported constructs

            Selected scopes:
            - Application `{{_request.Scope.Artifact}}`

            Outcome: Degraded. Owners: 4. Score: 2142 bp.

            ## model/orders.play — Command `sem1:0000000000000000000000000000000000000000000000000000000000000001`
            Semantic locator: Concept [Application: app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994, Declaration: Owner1]
            - STAGE-DEGRADE-002 (ThrowingOperationBody): Café é requirement
              Intent: Café é requirement Remedy: Implement and verify.
            ## model/orders.play — Query `sem1:0000000000000000000000000000000000000000000000000000000000000002`
            Semantic locator: Concept [Application: app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994, Declaration: Owner2]
            - STAGE-DEGRADE-005 (ThrowingOperationBody): Café é requirement
              Intent: Café é requirement Remedy: Implement and verify.
              Causes: `sem1:0000000000000000000000000000000000000000000000000000000000000001/STAGE-DEGRADE-002`
            ## model/orders.play — ReadModel `sem1:0000000000000000000000000000000000000000000000000000000000000003`
            Semantic locator: Concept [Application: app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994, Declaration: Owner3]
            - STAGE-DEGRADE-005 (ThrowingOperationBody): Café é requirement
              Intent: Café é requirement Remedy: Implement and verify.
              Causes: `sem1:0000000000000000000000000000000000000000000000000000000000000002/STAGE-DEGRADE-005`
            """ + "\n";
        Assert.True(Encoding.UTF8.GetBytes(expected).SequenceEqual(ArtifactRenderReports.Create(_plan, _context).Unsupported.Bytes));
    }
}
