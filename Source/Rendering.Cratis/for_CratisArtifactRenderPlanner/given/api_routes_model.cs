// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class api_routes_model
{
    internal const string Source = """
        module HTTPPayments
          feature Recording
            slice StateChange RecordPayment
              command RecordPayment
                id String identifier
                text String
                produces PaymentRecorded
                  for id
                  text = text
              event PaymentRecorded
                text String
            slice StateChange CorrectPayment
              command CorrectPayment
                id String identifier
                text String
                produces PaymentRecorded
                  for id
                  text = text
              command CancelPayment
                id String identifier
                text String
                produces PaymentRecorded
                  for id
                  text = text
            slice StateView Lookup
              readmodel PaymentTotals
                id String key
                total String
              query Total => PaymentTotals?
                by id String
              projection Build => PaymentTotals
                from PaymentRecorded
                  id = $eventSourceId
                  total = text
            slice StateView Details
              readmodel PaymentDetails
                id String key
                text String
              query PaymentById => PaymentDetails?
                by id String
              projection BuildDetails => PaymentDetails
                from PaymentRecorded
                  id = $eventSourceId
                  text = text
        """;
}
