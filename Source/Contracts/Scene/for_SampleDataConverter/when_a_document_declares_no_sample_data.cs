// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_a_document_declares_no_sample_data : given.a_translated_application
{
    void Because() => Translate(Model);

    [Fact] void should_have_no_packs() => _application.SampleDataPacks.ShouldBeEmpty();
    [Fact] void should_have_no_blueprint_packs() => _application.BlueprintSampleDataPacks.ShouldBeEmpty();
}
