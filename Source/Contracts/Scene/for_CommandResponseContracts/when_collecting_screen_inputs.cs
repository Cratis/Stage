// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts;

public class when_collecting_screen_inputs : Specification
{
    JsonObject _contracts = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sample"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("inputs"), "inputs", "Inputs.play", """
            module Sample
              feature Inputs
                slice StateChange Save
                  command Save
                    value String
                    returns value
                slice StateView Lookup
                  readmodel Item
                    id Uuid
                    value String
                  query ByText => Item?
                    by value String
                  query ByNumber => Item?
                    by value String
                  query All => Item[]
            """);
        var compilation = new SemanticModelCompiler().Compile("Sample", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var application = compilation.Value.Model.Application;
        var module = application.Modules[0];
        var feature = module.Features[0];
        var save = feature.Slices[0];
        var lookup = feature.Slices[1];
        var number = lookup.Queries[1];
        number = number with { Argument = number.Argument with { Type = number.Argument.Type with { Primitive = SemanticPrimitiveType.WholeNumber } } };
        lookup = lookup with { Queries = [lookup.Queries[0], number, lookup.Queries[2]] };
        feature = feature with { Slices = [save, lookup] };
        var first = lookup.Queries[0];
        var duplicate = first with { Argument = first.Argument with { Type = lookup.Queries[1].Argument.Type } };
        var later = lookup with { Queries = [duplicate], Commands = [save.Commands[0] with { Response = null }] };
        application = application with { Modules = [module with { Features = [feature with { Features = [feature with { Slices = [later], Features = [] }] }] }] };
        var scene = JsonNode.Parse("""
            {"screens":[{"name":"Details","children":[
              {"componentName":"core:data","properties":{"query":"ByText","by":"shared"}},
              [{"componentName":"core:data","properties":{"query":"ByNumber","by":"shared"}}],
              {"nested":{"componentName":"core:data","properties":{"query":"ByText","by":"second"}}},
              {"componentName":"core:data","properties":{"query":"All","by":"ignored"}},
              {"componentName":"core:data","properties":{"query":"Missing","by":"ignored"}},
              {"componentName":"core:data","properties":{"query":42,"by":"ignored"}},
              {"componentName":"core:data","properties":{"by":"ignored"}},
              {"componentName":"core:data","properties":{"query":"ByText","by":false}},
              {"componentName":"core:data","properties":{"query":"ByText"}},
              {"componentName":7,"properties":{"query":"ByText","by":"ignored"}}
            ]},{"name":7},{"children":[]}]}
            """).AsObject();
        _contracts = new CommandResponseContracts(application).From(scene, name => name, property => property.Name);
    }

    [Fact]
    void should_discover_nested_inputs_and_keep_last_write_in_first_insertion_order() =>
        Assert.Equal(/*lang=json*/ "{\"shared\":{\"kind\":\"number\"},\"second\":{\"kind\":\"string\"}}", _contracts["screenInputs"]["Details"].ToJsonString());

    [Fact]
    void should_ignore_missing_or_non_string_screen_names() => Assert.Single(_contracts["screenInputs"].AsObject());

    [Fact]
    void should_keep_the_first_command_definition() =>
        Assert.True(JsonNode.DeepEquals(_contracts["commands"]["Save"], JsonNode.Parse("""{"kind":"value","type":{"kind":"string"}}""")));
}
