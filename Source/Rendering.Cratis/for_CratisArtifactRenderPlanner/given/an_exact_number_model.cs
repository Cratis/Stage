// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class an_exact_number_model
{
    public const string Source = """
        concept RecordId : String
        concept Whole : Int
          validate
            <= 9007199254740991
            >= -9007199254740991
        module Numbers
          feature Recording
            slice StateChange Record
              command RecordNumbers
                id RecordId identifier
                largest Int
                smallest Int
                whole Int
                fraction Decimal
                validate
                  fraction >= 0.10000000000000002
                  require whole == 1234567890123456
                produces NumbersRecorded
                  for id
                  largest = largest
                  smallest = smallest
                  whole = whole
                  fraction = fraction
              event NumbersRecorded
                largest Int
                smallest Int
                whole Int
                fraction Decimal
              specification ExactValues
                when RecordNumbers
                  id = "numbers"
                  largest = 9007199254740991
                  smallest = -9007199254740991
                  whole = 1234567890123456
                  fraction = 0.10000000000000002
                then NumbersRecorded
                  largest = 9007199254740991
                  smallest = -9007199254740991
                  whole = 1234567890123456
                  fraction = 0.10000000000000002
                then readmodel RecordedNumbers
                  id = "numbers"
                  largest = 9007199254740991
                  smallest = -9007199254740991
                  whole = 1234567890123456
                  fraction = 0.10000000000000002
                  constantWhole = 1234567890123456
                  constantFraction = 0.10000000000000002
            slice StateView Lookup
              readmodel RecordedNumbers
                id RecordId
                largest Int
                smallest Int
                whole Int
                fraction Decimal
                constantWhole Int
                constantFraction Decimal
              projection NumbersProjection => RecordedNumbers
                from NumbersRecorded key $eventSourceId
                  largest = largest
                  smallest = smallest
                  whole = whole
                  fraction = fraction
                  constantWhole = 1234567890123456
                  constantFraction = 0.10000000000000002
              query ById => RecordedNumbers optional
                by id RecordId
        """;

    // Per-run projection execution admits event mappings but not constant mappings.
    // Keep the latter in Source for renderer emission coverage, separate from execution parity.
    public static string ExecutionSource => Source
        .Replace("          constantWhole = 1234567890123456\n          constantFraction = 0.10000000000000002\n", string.Empty, StringComparison.Ordinal)
        .Replace("        constantWhole Int\n        constantFraction Decimal\n", string.Empty, StringComparison.Ordinal);
}
