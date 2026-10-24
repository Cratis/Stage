// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_table_column_is_an_enumeration : Specification
{
    ExternalComponent _column = null!;

    void Because()
    {
        var scope = BehaviorScope.None with
        {
            Enumerations = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
            {
                ["Opportunity"] = new Dictionary<string, IReadOnlyList<string>> { ["stage"] = ["newLead", "inProgress", "won"] }
            }
        };
        var table = (ExternalComponent)ScreenDirectiveConverter.Convert(
            [new ScreenTableSyntax("Opportunity", [new ScreenColumnSyntax("stage", "Stage", SourceLocation.Start)], null, SourceLocation.Start)],
            "Pipeline",
            scope)[0];
        _column = (ExternalComponent)table.Slots["columns"].Single();
    }

    [Fact] void should_not_carry_numbers_for_an_implicitly_numbered_enumeration() => _column.Properties.ContainsKey("optionValues").ShouldBeFalse();
    [Fact] void should_carry_the_members_of_the_columns_read_model_property() => ((IReadOnlyList<string>)_column.Properties["options"]).ShouldContainOnly("newLead", "inProgress", "won");
}
