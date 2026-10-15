// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class optional_validated_composite_concept_cases
{
    const string Declarations = """
        concept Count : Int
          validate
            > 5 severity error message "Count minimum"
            < 10 severity warning message "Count maximum"
            != 2 severity information message "Count excluded"
        concept Amount : Decimal
          validate
            > 5 severity warning message "Amount minimum"
            < 10 severity error message "Amount maximum"
            != 2 severity information message "Amount excluded"
        concept Label : String
          validate
            length == 3 severity error message "Label length"
            matches "^X" severity warning message "Label pattern"
            != "bad" severity information message "Label excluded"
        type Details
          count Count optional
          amount Amount optional
          label Label optional
        type Node
          count Count optional
          next Node optional
        module Inputs
          feature Optional
        """;

    internal static ExecutableSemanticModel Compile()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OptionalComposites"));
        var source = Declarations + "\n" + string.Join('\n', Shapes().Select(SliceSource));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("optional-composites"),
            "optional-composites",
            "OptionalComposites.play",
            source);
        var compilation = new SemanticModelCompiler().Compile("OptionalComposites", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        return WithSlices(model, Slices(model).Select(slice => WithOptionalValues(model, slice)));
    }

    internal static ExecutableSemanticModel Admitted()
    {
        var model = Compile();
        return WithSlices(model, Slices(model).Select(slice => slice with
        {
            Specifications = [.. slice.Specifications.Where(specification => !specification.Name.EndsWith("NestedNull", StringComparison.Ordinal))]
        }));
    }

    internal static ExecutableSemanticModel ExplicitNull(string shape)
    {
        var model = Compile();
        var slice = Slices(model).Single(slice => string.Equals(slice.Name, $"{shape}Input", StringComparison.Ordinal));
        return WithSlices(model, [slice with
        {
            Specifications = [slice.Specifications.Single(specification => specification.Name.EndsWith("NestedNull", StringComparison.Ordinal))]
        }]);
    }

    internal static ExecutableSemanticModel Reordered()
    {
        var model = Compile();
        var slice = Slices(model).Single(slice => string.Equals(slice.Name, "OptionalDetailsInput", StringComparison.Ordinal));
        var specification = slice.Specifications.Single(specification => specification.Name.EndsWith("Invalid", StringComparison.Ordinal));
        var when = specification.When!;
        var reordered = specification with
        {
            When = when with { Values = [.. when.Values.Select(ReverseMembers)] },
            ThenErrors = [specification.ThenErrors.Single() with { Message = "Label pattern" }]
        };
        return WithSlices(model, [slice with { Specifications = [reordered] }]);
    }

    static SemanticPropertyValue ReverseMembers(SemanticPropertyValue value) =>
        value.Value is SemanticCompositeValue composite
            ? value with { Value = SemanticValue.Composite([.. composite.Properties.Reverse()]) }
            : value;

    static IEnumerable<InputShape> Shapes()
    {
        const string valid = "{\"count\":7,\"amount\":7.5,\"label\":\"Xxx\"}";
        const string invalid = "{\"count\":2,\"amount\":2,\"label\":\"bad\"}";
        return
        [
            new("RequiredDetails", "Details", valid, invalid, "{}"),
            new("OptionalDetails", "Details optional", valid, invalid, "{}"),
            new("Collection", "Details[] optional", $"[{valid},{valid}]", $"[{invalid},{invalid}]", "[{},{}]"),
            new(
                "Recursive",
                "Node optional",
                "{\"count\":7,\"next\":{\"count\":7}}",
                "{\"count\":2,\"next\":{\"count\":2}}",
                "{\"next\":{}}")
        ];
    }

    static string SliceSource(InputShape shape) => $$"""
            slice StateChange {{shape.Name}}Input
              command Check{{shape.Name}}
                id String
                value {{shape.Type}}
                returns id
        """ + "\n" + string.Join('\n', Cases(shape).Select(vector => SpecificationSource(shape, vector.Name, vector.Value)));

    static string SpecificationSource(InputShape shape, string name, string value) => $$"""
              specification {{shape.Name}}{{name}}
                when Check{{shape.Name}}
                  id = "one"
                  value = {{value}}
                {{(string.Equals(name, "Invalid", StringComparison.Ordinal) ? "then error \"Count minimum\"" : "then returns \"one\"")}}
        """;

    static IEnumerable<(string Name, string Value)> Cases(InputShape shape)
    {
        yield return ("NestedAbsent", shape.Absent);
        yield return ("Valid", shape.Valid);
        yield return ("Invalid", shape.Invalid);
        if (shape.Type.Contains("optional", StringComparison.Ordinal))
        {
            yield return ("ParentAbsent", shape.Absent);
        }

        if (string.Equals(shape.Name, "Collection", StringComparison.Ordinal))
        {
            yield return ("Empty", "[]");
        }

        if (string.Equals(shape.Name, "RequiredDetails", StringComparison.Ordinal) ||
            string.Equals(shape.Name, "OptionalDetails", StringComparison.Ordinal))
        {
            yield return ("NestedNull", "{}");
        }
    }

    static IEnumerable<SemanticSlice> Slices(ExecutableSemanticModel model) =>
        model.Application.Modules.Single().Features.Single().Slices;

    static ExecutableSemanticModel WithSlices(ExecutableSemanticModel model, IEnumerable<SemanticSlice> slices)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
        });
    }

    // Authored command fixtures reject null literals. Exercise the optional typed ESM contract too.
    static SemanticSlice WithOptionalValues(ExecutableSemanticModel model, SemanticSlice slice) => slice with
    {
        Specifications = [.. slice.Specifications.Select(specification =>
            OptionalSpecification(model, slice.Commands.Single(), specification))]
    };

    static SemanticSpecification OptionalSpecification(
        ExecutableSemanticModel model, SemanticCommand command, SemanticSpecification specification)
    {
        var when = specification.When!;
        var input = command.Properties.Single(property => string.Equals(property.Name, "value", StringComparison.Ordinal));
        return specification with
        {
            When = when with
            {
                Values = [.. when.Values.Select(value => value.TargetProperty == input.Id
                    ? value with { Value = OptionalValue(model, specification.Name, value.Value) } : value)]
            }
        };
    }

    static SemanticValue OptionalValue(ExecutableSemanticModel model, string name, SemanticValue value)
    {
        if (name.EndsWith("ParentAbsent", StringComparison.Ordinal))
        {
            return SemanticValue.Null;
        }

        if (name.EndsWith("NestedNull", StringComparison.Ordinal))
        {
            var details = model.Application.Types.Single(type => string.Equals(type.Name, "Details", StringComparison.Ordinal));
            return SemanticValue.Composite([.. details.Properties.Select(property =>
                new SemanticPropertyValue(property.Id, SemanticValue.Null))]);
        }

        return value;
    }

    sealed record InputShape(string Name, string Type, string Valid, string Invalid, string Absent);
}
#endif
