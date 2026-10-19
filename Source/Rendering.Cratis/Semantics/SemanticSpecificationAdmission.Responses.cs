// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks expected command responses that the generated specification compares by C# record equality.
/// Like event payloads, these are renderer-only fidelity limits; the executor and Host compare semantic values.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    const string CompositeResponseRejection = "An expected composite response can only be compared by C# record equality when every supplied member is a scalar or nested composite value in declaration order; explicit nulls and collection members are compared differently by the reference evaluator.";

    static bool HasComparableResponse(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        (specification.ThenReturns, command.Response) switch
        {
            (SemanticScalarSpecificationResponse expected, SemanticScalarCommandResponse response) => IsComparableResponse(context, expected.Value, response.Type),
            (SemanticRecordSpecificationResponse expected, SemanticRecordCommandResponse response) => expected.Fields.All(field =>
                response.Fields.SingleOrDefault(declared => declared.Name == field.Name) is not { } declared || IsComparableResponse(context, field.Value, declared.Type)),
            _ => true
        };

    static bool IsComparableResponse(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
        type.Kind != SemanticTypeReferenceKind.CompositeType || IsCompatible(value, type, context);
}
