// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class with_a_foreign_language_on_a_csharp_file : given.a_described_handler
{
    void Establish() => Resolve("python", "Handlers/ApplyLateFee.cs");
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_prefer_the_declared_language() => _classification.Kind.ShouldEqual(HandlerKind.Foreign);
}
