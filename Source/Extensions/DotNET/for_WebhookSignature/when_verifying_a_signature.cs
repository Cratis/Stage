// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Extensions.for_WebhookSignature;

public class when_verifying_a_signature : Specification
{
    const string Secret = "whsec_test";
    static readonly byte[] _body = Encoding.UTF8.GetBytes("{\"id\":\"evt_1\",\"amount\":100}");

    string _signature = null!;
    bool _valid;
    bool _prefixed;
    bool _upperCase;
    bool _tamperedBody;
    bool _wrongSecret;
    bool _missing;
    bool _empty;
    bool _truncated;
    bool _notHexadecimal;
    bool _emptySecret;

    void Because()
    {
        _signature = WebhookSignature.Compute(Secret, _body);
        _valid = WebhookSignature.Verify(Secret, _body, _signature);
        _prefixed = WebhookSignature.Verify(Secret, _body, "sha256=" + _signature);
        _upperCase = WebhookSignature.Verify(Secret, _body, _signature.ToUpperInvariant());
        _tamperedBody = WebhookSignature.Verify(Secret, Encoding.UTF8.GetBytes("{\"id\":\"evt_1\",\"amount\":999}"), _signature);
        _wrongSecret = WebhookSignature.Verify("another", _body, _signature);
        _missing = WebhookSignature.Verify(Secret, _body, null);
        _empty = WebhookSignature.Verify(Secret, _body, string.Empty);
        _truncated = WebhookSignature.Verify(Secret, _body, _signature[..^2]);
        _notHexadecimal = WebhookSignature.Verify(Secret, _body, new string('z', _signature.Length));
        _emptySecret = WebhookSignature.Verify(string.Empty, _body, WebhookSignature.Compute(string.Empty, _body));
    }

    [Fact] void should_compute_a_sha256_hmac_as_lowercase_hexadecimal() => (_signature.Length == 64 && _signature.All(character => char.IsAsciiDigit(character) || char.IsAsciiLetterLower(character))).ShouldBeTrue();
    [Fact] void should_accept_the_signature_of_the_raw_body() => _valid.ShouldBeTrue();
    [Fact] void should_accept_a_signature_with_the_algorithm_prefix() => _prefixed.ShouldBeTrue();
    [Fact] void should_accept_the_signature_in_either_case() => _upperCase.ShouldBeTrue();
    [Fact] void should_refuse_a_body_that_was_changed() => _tamperedBody.ShouldBeFalse();
    [Fact] void should_refuse_a_signature_made_with_another_secret() => _wrongSecret.ShouldBeFalse();
    [Fact] void should_refuse_a_missing_signature() => _missing.ShouldBeFalse();
    [Fact] void should_refuse_an_empty_signature() => _empty.ShouldBeFalse();
    [Fact] void should_refuse_a_truncated_signature() => _truncated.ShouldBeFalse();
    [Fact] void should_refuse_a_signature_that_is_not_hexadecimal() => _notHexadecimal.ShouldBeFalse();
    [Fact] void should_verify_nothing_without_a_secret() => _emptySecret.ShouldBeFalse();
}
