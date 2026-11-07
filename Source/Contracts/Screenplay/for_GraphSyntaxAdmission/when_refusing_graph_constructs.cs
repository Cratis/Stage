// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_GraphSyntaxAdmission;

public class when_refusing_graph_constructs : Specification
{
    const string Source = "module M\n  feature F\n    slice StateView S\n      event Registered\n        name String\n      graph G\n        node Person\n          personId String key\n          name String\n      projection P => graph G\n        node Person\n          from Registered key $eventSourceId\n            personId = $eventSourceId\n            name = name\n";

    [Theory]
    [InlineData(Source, "Graph 'G'")]
    [InlineData(Source + "      specification Registering\n        when append Registered\n          for \"p\"\n          name = \"Ada\"\n        then graph G\n          has node Person \"p\"\n            name = \"Ada\"\n", "Graph 'G'")]
    public void should_refuse_before_legacy_translation(string source, string construct)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var application = parsed.Value;
        var visited = Assert.IsType<InvalidEventModel>(Catch.Exception(() => new GraphSyntaxAdmission().VisitApplication(application)));
        var refusal = Assert.IsType<InvalidEventModel>(Catch.Exception(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(application)));
        refusal.Message.ShouldContain($"{GraphSyntaxAdmission.Code}:");
        Assert.Contains(construct, visited.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_a_graph_projection() =>
        Assert.Contains("Graph projection 'P'", Assert.IsType<InvalidEventModel>(Catch.Exception(() => new GraphSyntaxAdmission().VisitNode(new GraphProjectionSyntax("P", "G", [], SourceLocation.Start)))).Message, StringComparison.Ordinal);

    [Fact]
    public void should_refuse_a_graph_specification_step() =>
        Assert.Contains("Graph specification step 'G'", Assert.IsType<InvalidEventModel>(Catch.Exception(() => new GraphSyntaxAdmission().VisitNode(new SpecificationGraphSyntax("G", [], SourceLocation.Start)))).Message, StringComparison.Ordinal);

    [Fact]
    public void should_admit_an_application_without_graphs() =>
        Catch.Exception(() => new GraphSyntaxAdmission().VisitNode(new EventSyntax("Recorded", [], SourceLocation.Start))).ShouldBeNull();
}
