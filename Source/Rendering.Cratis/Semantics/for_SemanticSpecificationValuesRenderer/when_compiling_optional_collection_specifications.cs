// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationValuesRenderer;

public class when_compiling_optional_collection_specifications : Specification
{
    [Theory]
    [InlineData("Uuid", "\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", typeof(Guid))]
    [InlineData("Date", "\"2026-01-02\"", typeof(DateOnly))]
    [InlineData("DateTime", "\"2026-01-02T03:04:05Z\"", typeof(DateTimeOffset))]
    [InlineData("Int", "42", typeof(int))]
    [InlineData("Decimal", "12.5", typeof(decimal))]
    [InlineData("Bool", "true", typeof(bool))]
    public void should_compile_required_element_fixtures_without_losing_optional_containers_or_scalars(string primitive, string literal, Type elementType)
    {
        var model = invoice_model.Compile($$"""
            module Inputs
              feature Optional
                slice StateChange Check
                  command Check
                    id String
                    values {{primitive}}[] optional
                    scalar {{primitive}} optional
                    otherScalar {{primitive}} optional
                    returns id
                  specification Checking
                    when Check
                      id = "one"
                      values = [{{literal}}, {{literal}}]
                      scalar = {{literal}}
                      otherScalar = {{literal}}
                    then returns "one"
            """);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        RenderedOutput.Warnings(files).ShouldBeEmpty();
        var assembly = RenderedOutput.Load(files);
        var specification = assembly.GetTypes().Single(type => type.Name == "when_checking");
        var fixtures = specification.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(field => field.IsInitOnly).ToArray();
        fixtures.Select(field => field.FieldType).ShouldContainOnly(typeof(string), elementType, typeof(Nullable<>).MakeGenericType(elementType));
        var command = assembly.GetTypes().Single(type => type.Name == "Check");
        var collection = command.GetProperty("Values")!;
        collection.PropertyType.ShouldEqual(typeof(IReadOnlyList<>).MakeGenericType(elementType));
        new NullabilityInfoContext().Create(collection).ReadState.ShouldEqual(NullabilityState.Nullable);
        command.GetProperty("Scalar")!.PropertyType.ShouldEqual(typeof(Nullable<>).MakeGenericType(elementType));
    }
}
#endif
