// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_generation_model
{
    public const string Source = """
        concept Id : Uuid
          generate uuidv7
        concept Receipt : String
          generate nanoid
        concept StringUuid : String
          generate uuid case upper prefix "ref_"
        concept SortableId : String
          generate ulid case lower
        concept UserId : String
          generate typeid "user"
        concept BareTypeId : String
          generate typeid ""
        concept BinaryCode : String
          generate nanoid 64 alphabet "ab" prefix "code_"
        concept EncodedRandom : String
          generate bytes 32 base64url
        concept HexReference : String
          generate bytes 16 hex case upper
        concept Base32Reference : String
          generate bytes 16 base32 case lower
        concept ImplicitId : Uuid
        module Billing
          feature Creation
            slice StateChange Prior
              event Unrelated
              command RecordPrior
                id Id identifier
                produces Unrelated
                  for id
            slice StateChange Start
              event Started
                receipt Receipt
              event Initialized
              command Create
                id Id generated identifier
                receipt Receipt generated
                stringUuid StringUuid generated
                sortableId SortableId generated
                userId UserId generated
                bareTypeId BareTypeId generated
                binaryCode BinaryCode generated
                encodedRandom EncodedRandom generated
                hexReference HexReference generated
                base32Reference Base32Reference generated
                implicitId ImplicitId generated
                produces Started
                  for id
                  receipt = receipt
                produces Initialized
                  for id
                returns receipt
        """;

    public const string Fixtures = """
                  for "00112233-4455-7677-8899-aabbccddeeff"
                  generated receipt = "ABCDEFGHIJKLMNOPQRSTU"
                  generated stringUuid = "ref_00112233-4455-4677-8899-AABBCCDDEEFF"
                  generated sortableId = "01arz3ndektsv4rrffq69g5fav"
                  generated userId = "user_0000000000e008000000000000"
                  generated bareTypeId = "0000000000e008000000000000"
                  generated binaryCode = "code_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                  generated encodedRandom = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
                  generated hexReference = "0123456789ABCDEF0123456789ABCDEF"
                  generated base32Reference = "aaaaaaaaaaaaaaaaaaaaaaaaaa"
                  generated implicitId = "00112233-4455-4677-8899-aabbccddeeff"
        """;

    public static ExecutableSemanticModel Model() => invoice_model.Compile(Source + "\n" + $$"""
              specification SeveralInitialFacts
                when Create
        {{Fixtures}}
                then Started
                  for "00112233-4455-7677-8899-aabbccddeeff"
                  receipt = "ABCDEFGHIJKLMNOPQRSTU"
                then Initialized
                  for "00112233-4455-7677-8899-aabbccddeeff"
                then returns "ABCDEFGHIJKLMNOPQRSTU"
              specification ReusedSource
                given Unrelated
                  for "00112233-4455-7677-8899-aabbccddeeff"
                when Create
        {{Fixtures}}
                then error "A generated event source already has facts; retry the command with a fresh generated value."
        """);

    public static ArtifactRenderPlan Plan()
    {
        var model = Model();

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
    }
}
