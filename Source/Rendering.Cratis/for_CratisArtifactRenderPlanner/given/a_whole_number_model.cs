// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_whole_number_model
{
    // Screenplay's binder loses precision in literals with 16+ significant digits until Cratis/Screenplay#639.
    // Use exactly bound 15-digit literals here; executable-model specs cover the exact ±(2^53−1) extremes.
    internal const string Source = """
        concept StockId : String
        concept Quantity : Int
        module Inventory
          feature Stock
            slice StateChange Record
              command RecordStock
                id StockId identifier
                quantity Quantity
                count Int
                produces StockRecorded
                  for id
                  quantity = quantity
                  count = count
              event StockRecorded
                quantity Quantity
                count Int
              specification RecordingPositive
                when RecordStock
                  id = "positive"
                  quantity = 900719925474099
                  count = 900719925474099
                then StockRecorded
                  quantity = 900719925474099
                  count = 900719925474099
                then readmodel Stock
                  id = "positive"
                  quantity = 900719925474099
                  count = 900719925474099
              specification RecordingNegative
                when RecordStock
                  id = "negative"
                  quantity = -900719925474099
                  count = -900719925474099
                then StockRecorded
                  quantity = -900719925474099
                  count = -900719925474099
                then readmodel Stock
                  id = "negative"
                  quantity = -900719925474099
                  count = -900719925474099
            slice StateView Stocks
              readmodel Stock
                id StockId
                quantity Quantity
                count Int
              projection StockProjection => Stock
                from StockRecorded key $eventSourceId
                  quantity = quantity
                  count = count
              query ById => Stock optional
                by id StockId
        """;

    internal static ExecutableSemanticModel Compile(SemanticVersion version, string? source = null)
    {
        var marker = version.IsAtLeast(SemanticVersion.V8)
            ? "eventsource VersionMarker\n  identifier String\n"
            : "policy VersionMarker\n  require not authenticated\n";
        return invoice_model.Compile(marker + (source ?? Source));
    }
}
