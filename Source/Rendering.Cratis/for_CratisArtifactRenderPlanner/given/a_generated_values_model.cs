// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_generated_values_model
{
    public const string Source = """
        concept ProjectId : Uuid
        concept Receipt : Uuid
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId ProjectId generated identifier
                name String
                produces event ProjectRegistered
                  projectId ProjectId = projectId
                  name String = name
                returns projectId
              specification RegisteringWithAPin
                when RegisterProject
                  for "11111111-1111-1111-1111-111111111111"
                  name = "Apollo"
                then ProjectRegistered
                  projectId = "11111111-1111-1111-1111-111111111111"
                  name = "Apollo"
                then returns "11111111-1111-1111-1111-111111111111"
              specification RegisteringWithoutAPin
                when RegisterProject
                  name = "Apollo"
                then ProjectRegistered
                  projectId = "11111111-1111-1111-1111-111111111111"
                  name = "Apollo"
                then returns "11111111-1111-1111-1111-111111111111"
            slice StateChange RegisterReceipt
              command RegisterReceipt
                projectId ProjectId generated identifier
                receipt Receipt generated
                name String
                produces event ReceiptRegistered
                  projectId ProjectId = projectId
                  receipt Receipt = receipt
                  name String = name
                returns
                  projectId = projectId
                  receipt = receipt
                  name = name
              specification RegisteringAReceipt
                when RegisterReceipt
                  for "11111111-1111-1111-1111-111111111111"
                  generated receipt = "22222222-2222-2222-2222-222222222222"
                  name = "Apollo"
                then ReceiptRegistered
                  projectId = "11111111-1111-1111-1111-111111111111"
                  receipt = "22222222-2222-2222-2222-222222222222"
                  name = "Apollo"
                then returns
                  projectId = "11111111-1111-1111-1111-111111111111"
                  receipt = "22222222-2222-2222-2222-222222222222"
                  name = "Apollo"
            slice StateChange AllocateReceipt
              command AllocateReceipt
                receipt Receipt generated
                returns receipt
              specification AllocatingAReceipt
                when AllocateReceipt
                  generated receipt = "22222222-2222-2222-2222-222222222222"
                then returns "22222222-2222-2222-2222-222222222222"
        """;
}
