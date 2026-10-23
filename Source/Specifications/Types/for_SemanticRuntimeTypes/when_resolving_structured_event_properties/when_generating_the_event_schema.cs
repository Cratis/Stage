// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.when_resolving_structured_event_properties;

public class when_generating_the_event_schema : given.a_structured_event_contract
{
    string _schema = string.Empty;

    void Because() => _schema = new Defaults(new SemanticClientArtifactsProvider([_eventType])).JsonSchemaGenerator.Generate(_eventType).ToJson();

    [Fact] void should_describe_decimals_precisely() => _schema.ShouldContain("decimal?");
    [Fact] void should_describe_the_collection_items() => _schema.ShouldContain("items");
}
