// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;

namespace Cratis.Stage.Host.for_StageJourneyMatching.given;

public class a_comparison : Specification
{
    private protected IReadOnlyList<StageJourneyMismatch> _mismatches = [];

    private protected void Compare(string expected, string actual, StageJourneyListMatch list = StageJourneyListMatch.Contains) =>
        _mismatches = StageJourneyMatching.Compare(Json(expected), Json(actual), list);

    protected static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);

        return document.RootElement.Clone();
    }
}
