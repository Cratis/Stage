// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Scriban;
using Scriban.Runtime;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_LockedBuiltins;

public class when_sorting_mixed_numeric_keys : Specification
{
    string _result = null!;

    void Because()
    {
        var budget = TemplateBudget.Default;
        var context = new LockedContext(budget, new PlanMeter(budget), CancellationToken.None);
        var scope = new ScriptObject();
        var first = new ScriptObject { ["key"] = 1.23m, ["name"] = "decimal" };
        var second = new ScriptObject { ["key"] = 1.23d, ["name"] = "double" };
        scope.SetValue("values", new ScriptArray { first, second }, true);
        context.PushGlobal(scope);
        _result = Template.Parse("{{ for item in (array.sort values 'key') }}{{ item.name }} {{ end }}").Render(context);
    }

    [Fact] void should_preserve_stable_order_for_equal_numeric_keys() => _result.ShouldEqual("decimal double ");
}
