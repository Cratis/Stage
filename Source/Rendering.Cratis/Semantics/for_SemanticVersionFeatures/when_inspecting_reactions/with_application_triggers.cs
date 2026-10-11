// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_application_triggers : Specification
{
    SemanticVersionFeature _feature = null!;

    void Because() => _feature = SemanticVersionFeatures.InApplication(SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan()).Application).Single();

    [Fact] void should_keep_the_application_trigger_code() => _feature.Code.ShouldEqual("STAGE-ESM-024");
    [Fact] void should_name_the_application_trigger() => _feature.Message.ShouldEqual("Application trigger 'PaymentFileArrived' is an ESM v6 automation construct, which Stage does not support yet.");
}
