// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_compiling_enum_units : Specification
{
    string _cache = null!;

    void Establish() => _cache = Path.Combine(AppContext.BaseDirectory, "body-unit-cache", Guid.NewGuid().ToString("N"));

    void Destroy()
    {
        if (Directory.Exists(_cache)) Directory.Delete(_cache, recursive: true);
    }

    [Theory]
    [InlineData(SemanticImplementationRole.RulePredicate)]
    [InlineData(SemanticImplementationRole.CommandValidation)]
    [InlineData(SemanticImplementationRole.ConceptValidation)]
    void should_cache_authored_enum_names_for_every_validation_role(SemanticImplementationRole role)
    {
        var source = role switch
        {
            SemanticImplementationRole.RulePredicate => an_enum_validation_model.Command("return context.Value == global::InvoiceModel.Common.Status.Open;"),
            SemanticImplementationRole.CommandValidation => an_enum_validation_model.Command("if (context.Artifact.Status != global::InvoiceModel.Common.Status.Open) yield return \"Closed\";", iterator: true),
            _ => an_enum_validation_model.Enumeration + validation_body_model.IteratorConcept("if (context.Value <= 0) yield return \"Positive amount required\";")
        };
        var loaded = validation_body_model.Load(source);
        var unit = Assert.Single(BodyUnitCompiler.Compile(loaded, _cache).Entries);

        Assert.Equal(role.ToString(), unit.Role);
        AssertEnumNames(unit, BodyUnitCompiler.CreateContext(loaded).CommonNamespace);
    }

    [Fact]
    void should_preserve_reducer_enum_names()
    {
        var loaded = validation_body_model.Load(an_enum_validation_model.Enumeration + a_reducer_fact_specification.Source);
        var unit = Assert.Single(BodyUnitCompiler.Compile(loaded, _cache).Entries);

        Assert.Equal(nameof(SemanticImplementationRole.ReducerTransition), unit.Role);
        AssertEnumNames(unit, BodyUnitCompiler.CreateContext(loaded).CommonNamespace);
    }

    [Fact]
    void should_keep_the_mapping_empty_without_enumerations()
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        var unit = Assert.Single(BodyUnitCompiler.Compile(loaded, _cache).Entries);

        Assert.True(unit.Admitted, unit.DiagnosticMessage);
        Assert.Empty(unit.EnumValues);
        var cached = BodyUnitCache.Read(_cache, unit.UnitHash!);
        Assert.NotNull(cached);
        Assert.Empty(cached.EnumValues);
    }

    void AssertEnumNames(BodyUnitEntry unit, string commonNamespace)
    {
        Assert.True(unit.Admitted, unit.DiagnosticMessage);
        Assert.Equal(2, unit.EnumValues.Count);
        Assert.Equal("open", unit.EnumValues[$"{commonNamespace}.Status.Open"]);
        Assert.Equal("closed", unit.EnumValues[$"{commonNamespace}.Status.Closed"]);
        var cached = BodyUnitCache.Read(_cache, unit.UnitHash!);
        Assert.NotNull(cached);
        Assert.Equal(unit.EnumValues.OrderBy(pair => pair.Key), cached.EnumValues.OrderBy(pair => pair.Key));
    }
}
#endif
