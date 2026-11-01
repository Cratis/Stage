// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Specifications;
using Scriban;
using Scriban.Runtime;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_LockedBuiltins;

public class when_sorting_with_cancellation : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_check_cancellation_during_materialization_and_comparison(bool afterMaterialization)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = TemplateBudget.Default;
        var context = new LockedContext(budget, new PlanMeter(budget), cancellation.Token);
        var scope = new ScriptObject();
        scope.SetValue("values", new ScriptRange(Values(cancellation, afterMaterialization)), true);
        context.PushGlobal(scope);
        var error = Catch.Exception(() => Template.Parse("{{ array.sort values }}").Render(context));
        error.ShouldNotBeNull();
        TemplateErrorMap.Map(error).Code.ShouldEqual(TemplateCodes.Deadline);
    }

    static IEnumerable Values(CancellationTokenSource cancellation, bool afterMaterialization)
    {
        yield return 2;
        if (!afterMaterialization) cancellation.Cancel();
        yield return 1;
        cancellation.Cancel();
    }
}
