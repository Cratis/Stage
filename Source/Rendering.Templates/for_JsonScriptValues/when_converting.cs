// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Scriban.Runtime;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_JsonScriptValues;

public class when_converting : Specification
{
    ScriptObject _value = null!;

    void Because()
    {
        using var document = JsonDocument.Parse("{\"count\":12,\"price\":1.25,\"items\":[true,null,\"ok\"]}");
        _value = (ScriptObject)JsonScriptValues.Convert(document.RootElement, 100)!;
    }

    [Fact] void should_keep_integral_numbers() => _value["count"].ShouldBeOfExactType<long>();
    [Fact] void should_keep_decimal_numbers() => _value["price"].ShouldEqual(1.25m);
    [Fact] void should_freeze_objects() => _value.IsReadOnly.ShouldBeTrue();
    [Fact] void should_freeze_nested_arrays() => ((ScriptArray)_value["items"]).IsReadOnly.ShouldBeTrue();
    [Fact] void should_retain_json_after_document_disposal() => ((JsonScriptValues.JsonObject)_value).Source.GetProperty("count").GetInt64().ShouldEqual(12);

    [Theory]
    [InlineData("\"\\ue000\"", TemplateCodes.JsonPrivateUse)]
    [InlineData("{\"\\udb80\\udc00\":1}", TemplateCodes.JsonPrivateUse)]
    [InlineData("1e100", TemplateCodes.Input)]
    [InlineData("\"too long\"", TemplateCodes.Input)]
    public void should_refuse_invalid_values(string json, string code)
    {
        using var document = JsonDocument.Parse(json);
        var refusal = Catch.Exception(() => JsonScriptValues.Convert(document.RootElement, 4));
        ((TemplateRefusal)refusal).Diagnostic.Code.ShouldEqual(code);
    }

    [Fact]
    public void should_bound_json_depth()
    {
        using var document = JsonDocument.Parse(new string('[', 65) + "0" + new string(']', 65), new JsonDocumentOptions { MaxDepth = 100 });
        ((TemplateRefusal)Catch.Exception(() => JsonScriptValues.Convert(document.RootElement, 100))).Diagnostic.Code.ShouldEqual(TemplateCodes.Input);
    }
}
