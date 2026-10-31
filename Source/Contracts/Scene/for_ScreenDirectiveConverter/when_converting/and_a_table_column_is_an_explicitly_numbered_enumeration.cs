// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_table_column_is_an_explicitly_numbered_enumeration : Specification
{
    ExternalComponent _column = null!;

    void Because()
    {
        var scope = BehaviorScope.None with
        {
            Enumerations = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
            {
                ["Opportunity"] = new Dictionary<string, IReadOnlyList<string>> { ["stage"] = ["draft", "reversed", "sent"] }
            },
            EnumerationNumbers = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<int>>>
            {
                ["Opportunity"] = new Dictionary<string, IReadOnlyList<int>> { ["stage"] = [-1, 4, 90] }
            }
        };
        var table = (ExternalComponent)ScreenDirectiveConverter.Convert(
            [new ScreenTableSyntax("Opportunity", [new ScreenColumnSyntax("stage", "Stage", SourceLocation.Start)], null, SourceLocation.Start)],
            "Pipeline",
            scope)[0];
        _column = (ExternalComponent)table.Slots["columns"].Single();
    }

    [Fact] void should_carry_the_members_of_the_columns_read_model_property() => ((IReadOnlyList<string>)_column.Properties["options"]).ShouldContainOnly("draft", "reversed", "sent");
    [Fact] void should_carry_the_number_of_each_member_including_negative_and_sparse_ones() => ((IReadOnlyList<int>)_column.Properties["optionValues"]).ShouldContainOnly(-1, 4, 90);
}
