// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_two_framework_namespaces_share_a_type_name : Specification
{
    string _forward = null!;
    string _reversed = null!;

    void Because()
    {
        _forward = Render("global::System.Threading.Timer First, global::System.Timers.Timer Second");
        _reversed = Render("global::System.Timers.Timer Second, global::System.Threading.Timer First");
    }

    [Fact] void should_keep_the_ordinal_winner_short() => _forward.ShouldContain("Order(Timer First, global::System.Timers.Timer Second)");
    [Fact] void should_keep_the_same_winner_when_reference_order_changes() => _reversed.ShouldContain("Order(global::System.Timers.Timer Second, Timer First)");
    [Fact] void should_not_leave_both_colliding_types_qualified() => _forward.Contains("global::System.Threading.Timer", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_import_the_namespace_of_the_qualified_loser() => _forward.Contains("using System.Timers;", StringComparison.Ordinal).ShouldBeFalse();

    static string Render(string parameters)
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [], []));
        return builder.Namespace("Shop.Orders").Line($"public record Order({parameters});").ToString();
    }
}
