// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_PackageSignatureVerifier;

public class when_verifying_signatures : Specification
{
    const string Digest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Other = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    SignatureVerdict _signedByThePublisher;
    SignatureVerdict _signedByAnotherKey;
    SignatureVerdict _signedForAnotherPackage;
    SignatureVerdict _unsigned;
    SignatureVerdict _tampered;
    SignatureVerdict _pemKey;
    SignatureVerdict _unusableKey;

    void Because()
    {
        using var publisher = PackageFixtures.NewKey();
        using var stranger = PackageFixtures.NewKey();
        static bool covers(byte[] signed) => PackageSignatureVerifier.PayloadNamesDigest(signed, Digest);
        var base64 = Convert.ToBase64String(publisher.ExportSubjectPublicKeyInfo());
        var payload = PackageFixtures.CosignPayload(Digest);
        var signature = PackageFixtures.Sign(publisher, payload);

        _signedByThePublisher = PackageSignatureVerifier.Verify([signature], [base64], covers);
        _pemKey = PackageSignatureVerifier.Verify([signature], [PackageFixtures.Pem(publisher)], covers);
        _signedByAnotherKey = PackageSignatureVerifier.Verify([PackageFixtures.Sign(stranger, payload)], [base64], covers);
        _signedForAnotherPackage = PackageSignatureVerifier.Verify([PackageFixtures.Sign(publisher, PackageFixtures.CosignPayload(Other))], [base64], covers);
        _unsigned = PackageSignatureVerifier.Verify([], [base64], covers);
        _tampered = PackageSignatureVerifier.Verify([new([.. PackageFixtures.CosignPayload(Digest), (byte)' '], signature.Signature)], [base64], covers);
        _unusableKey = PackageSignatureVerifier.Verify([signature], ["not a key"], covers);
    }

    [Fact] void should_accept_a_signature_by_the_publishers_key() => _signedByThePublisher.ShouldEqual(SignatureVerdict.Valid);
    [Fact] void should_accept_a_pem_key() => _pemKey.ShouldEqual(SignatureVerdict.Valid);
    [Fact] void should_reject_a_signature_by_another_key() => _signedByAnotherKey.ShouldEqual(SignatureVerdict.Invalid);
    [Fact] void should_reject_a_valid_signature_of_another_package() => _signedForAnotherPackage.ShouldEqual(SignatureVerdict.Invalid);
    [Fact] void should_report_a_package_without_signatures_as_unsigned() => _unsigned.ShouldEqual(SignatureVerdict.NotSigned);
    [Fact] void should_reject_a_payload_that_was_changed_after_signing() => _tampered.ShouldEqual(SignatureVerdict.Invalid);
    [Fact] void should_reject_when_no_key_is_usable() => _unusableKey.ShouldEqual(SignatureVerdict.Invalid);
}
