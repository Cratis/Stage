// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_whole_number_model
{
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
                  quantity = 9007199254740991
                  count = 9007199254740991
                then StockRecorded
                  quantity = 9007199254740991
                  count = 9007199254740991
                then readmodel Stock
                  id = "positive"
                  quantity = 9007199254740991
                  count = 9007199254740991
              specification RecordingNegative
                when RecordStock
                  id = "negative"
                  quantity = -9007199254740991
                  count = -9007199254740991
                then StockRecorded
                  quantity = -9007199254740991
                  count = -9007199254740991
                then readmodel Stock
                  id = "negative"
                  quantity = -9007199254740991
                  count = -9007199254740991
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

        // Earlier source fixtures must genuinely select their released version, not relabel a v11 model.
        var numbers = version.IsAtLeast(SemanticVersion.V11) ? Source : Source.Replace("9007199254740991", "900719925474099", StringComparison.Ordinal);

        var model = invoice_model.Compile(marker + (source ?? numbers));
        model.LanguageVersion.ShouldEqual(new LanguageVersion(version.Major, version.Minor));
        model.SemanticVersion.ShouldEqual(version);

        return model;
    }
}
