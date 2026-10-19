// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Forms;
using Cratis.Specifications;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Api.for_CommandFormRuntimeMetadata;

public class when_placing_a_form_that_populates_via_a_query : Specification
{
    SceneElements.ExternalComponent _result = null!;
    PopulateViaQuery _source = null!;

    void Establish() => _source = new PopulateViaQuery("GetCheckoutDraft", new Dictionary<string, BindingExpression> { ["basketId"] = new("basketId") });

    void Because()
    {
        var placed = new SceneElements.ExternalComponent
        {
            Id = "checkout",
            Name = "checkout",
            ComponentName = "Cratis.Components:commandForm",
            Properties = new Dictionary<string, object?> { ["command"] = "CheckOut" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>()
        };
        var form = new Form("CheckOutForm", "CheckOut", _source, [], FormGenerationMode.Auto);
        _result = CommandFormRuntimeMetadata.PlacedFormElement(placed, form, CommandFormRuntimeMetadata.FromSchema("CheckOut", """{"type":"object","properties":{"basketId":{"type":"string"}}}"""));
    }

    [Fact] void should_carry_the_population_source_to_the_form_the_frontend_renders() => _result.Properties["populateSource"].ShouldEqual(_source);
}
