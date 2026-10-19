// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReducerMigrationEvidence;

public class when_reading_an_existing_reducer_file
{
    static readonly SemanticId _readModel = SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact]
    public void should_use_the_full_type_name_without_an_identifier()
    {
        const string content = "namespace Projects.Orders;\npublic class Fold : global::Cratis.Chronicle.Reducers.IReducerFor<global::Projects.Orders.Total> { }\n";
        var evidence = ReducerMigrationEvidence.FromExistingFile(_readModel, "Orders\\Fold.cs", content.Replace("\n", "\r\n", StringComparison.Ordinal));
        Assert.Equal("Projects.Orders.Fold", evidence.ObserverId);
        Assert.Equal("Projects.Orders.Total", evidence.ReadModelType);
        Assert.Equal("Orders/Fold.cs", evidence.RelativePath);
        Assert.Equal(PlannedArtifact.CreateText("Orders/Fold.cs", content).Sha256, evidence.Sha256);
    }

    [Fact]
    public void should_separate_nested_types_like_the_clr()
    {
        const string content = "namespace Projects { namespace Orders { public static class Outer { public class Fold : IReducerFor<Total> { } } } }";
        Assert.Equal("Projects.Orders.Outer+Fold", ReducerMigrationEvidence.FromExistingFile(_readModel, "Orders/Fold.cs", content).ObserverId);
    }

    [Theory]
    [InlineData("[Reducer(\"orders-total\")]", "orders-total")]
    [InlineData("[global::Cratis.Chronicle.Reducers.ReducerAttribute(id: \"orders-total\")]", "orders-total")]
    [InlineData("[Reducer]", "Projects.Orders.Fold")]
    [InlineData("[Reducer(\"\")]", "Projects.Orders.Fold")]
    public void should_use_a_literal_reducer_identifier(string attribute, string expected)
    {
        var content = $"namespace Projects.Orders;\n{attribute}\npublic class Fold : IReducerFor<Total> {{ }}\n";
        Assert.Equal(expected, ReducerMigrationEvidence.FromExistingFile(_readModel, "Orders/Fold.cs", content).ObserverId);
    }

    [Theory]
    [InlineData("namespace Projects.Orders;\npublic class Fold : IReducerFor<Total> {")]
    [InlineData("namespace Projects.Orders;\npublic class Fold { }")]
    [InlineData("namespace Projects.Orders;\npublic class Fold : IReducerFor<Total> { }\npublic class Other : IReducerFor<Total> { }")]
    [InlineData("namespace Projects.Orders;\n[Reducer(Names.Id)]\npublic class Fold : IReducerFor<Total> { }")]
    public void should_refuse_files_without_one_literal_reducer(string content) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerMigrationEvidence.FromExistingFile(_readModel, "Orders/Fold.cs", content));

    [Fact]
    public void should_refuse_an_unportable_path() =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerMigrationEvidence.FromExistingFile(_readModel, "../Fold.cs", "public class Fold : IReducerFor<Total> { }"));
}
#endif
