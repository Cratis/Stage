// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;

public class compliance_concepts : Specification
{
    internal const string Source = """
        concept AccountId : Uuid
        concept BarePersonal : String pii
        concept ReasonPersonal : String personal
          personal reason "A contact address."
        concept Origin : String pii
          pii special racialOrEthnicOrigin
        concept Opinions : String pii
          pii special politicalOpinions
        concept Beliefs : String pii
          pii special religiousOrPhilosophicalBeliefs
        concept Membership : String pii
          pii special tradeUnionMembership
        concept Genetic : String pii
          pii special genetic
        concept Biometric : String pii
          pii special biometric
        concept Health : String pii
          pii special health
        concept Sexuality : String pii
          pii special sexLifeOrSexualOrientation
        concept Convictions : String pii
          pii criminal
        concept MedicalNote : String pii
          pii special health
          pii criminal
          pii reason "Clinical evidence."
        concept PersonalSecret : String pii secret
          pii reason "Personal credential."
          secret reason "Do not expose."
        concept BareSecret : String secret
        concept ReasonSecret : String secret
          secret reason "A credential."
        concept SubjectSecret : String secret
          secret scope subject
        concept NamespaceSecret : String secret
          secret scope namespace
          secret reason "Shared credential."
        concept GlobalSecret : String secret
          secret scope global
          secret reason "Installation credential."
        module Accounts
          feature Credentials
            slice StateChange SetCredential
              command SetCredential
                accountId AccountId identifier
                medicalNote MedicalNote
                namespaceSecret NamespaceSecret
                personalSecret PersonalSecret
                produces CredentialSet
                  for accountId
                  medicalNote = medicalNote
                  namespaceSecret = namespaceSecret
                  personalSecret = personalSecret
              event CredentialSet
                medicalNote MedicalNote
                namespaceSecret NamespaceSecret
                personalSecret PersonalSecret
        """;

    protected ApplicationSet _applicationSet = null!;

    void Establish()
    {
        var compilation = new ScreenplayCompiler().Compile(Source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        compilation.Diagnostics.ShouldBeEmpty();
        _applicationSet = new([compilation.Value!]);
    }
}
