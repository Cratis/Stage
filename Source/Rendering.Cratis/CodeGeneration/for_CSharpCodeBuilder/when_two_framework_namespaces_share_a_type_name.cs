// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_two_framework_namespaces_share_a_type_name
{
    [Theory]
    [InlineData("global::System.Threading.Timer First, global::System.Timers.Timer Second")]
    [InlineData("global::System.Timers.Timer Second, global::System.Threading.Timer First")]
    void should_bind_the_same_framework_identities_independent_of_reference_order(string parameters)
    {
        var source = new CSharpCodeBuilder(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [], []))
            .Namespace("Shop.Orders").Line($"public record Order({parameters});").ToString();
        Assert.Contains("using StageImport0 = global::System.Timers;", source);
        Assert.DoesNotContain("using System.Timers;", source);
        Assert.DoesNotContain("global::System.Threading.Timer", source);
        var tree = CSharpSyntaxTree.ParseText(SemanticBodyCompilation.ImplicitUsings + source);
        var compilation = SemanticBodyCompilation.Create("TimerAliases", tree);
        Assert.Empty(compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
        var order = compilation.GetTypeByMetadataName("Shop.Orders.Order")!;
        var first = (IPropertySymbol)Assert.Single(order.GetMembers("First"));
        var second = (IPropertySymbol)Assert.Single(order.GetMembers("Second"));
        Assert.True(SymbolEqualityComparer.Default.Equals(first.Type, compilation.GetTypeByMetadataName("System.Threading.Timer")));
        Assert.True(SymbolEqualityComparer.Default.Equals(second.Type, compilation.GetTypeByMetadataName("System.Timers.Timer")));
    }
}
