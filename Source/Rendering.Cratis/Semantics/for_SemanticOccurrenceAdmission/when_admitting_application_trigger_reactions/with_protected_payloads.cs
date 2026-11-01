// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_protected_payloads : given.an_application_trigger_reaction
{
    readonly Dictionary<(string Marking, string PayloadType), (string Message, bool Admitted)> _results = [];

    void Because()
    {
        foreach (var marking in new[] { "pii", "secret" })
        {
            foreach (var payloadType in new[] { "PrivateBatchId", "Envelope" })
            {
                var prefix = $"concept PrivateBatchId : String {marking}\ntype Envelope\n  value PrivateBatchId\n";
                var source = Source.Replace("  batchId String\nmodule", $"  batchId String\n  payload {payloadType}\nmodule", StringComparison.Ordinal)
                    .Replace("          batchId\n", "          batchId\n          payload\n", StringComparison.Ordinal);
                InspectSource(prefix + source);
                var diagnostic = _diagnostics.Single(item => item.Code == "STAGE-ESM-024" && item.Artifact == _reaction.Id);
                _results[(marking, payloadType)] = (diagnostic.Message, SemanticOccurrenceAdmission.AdmittedTriggers(_context).Any());
            }
        }
    }

    [Theory]
    [InlineData("pii", "PrivateBatchId")]
    [InlineData("secret", "PrivateBatchId")]
    [InlineData("pii", "Envelope")]
    [InlineData("secret", "Envelope")]
    public void should_refuse_plaintext_personal_or_secret_occurrence_payloads(string marking, string payloadType)
    {
        var result = _results[(marking, payloadType)];
        result.Message.ShouldContain("Application trigger 'BatchRequested' property 'payload'");
        result.Message.ShouldContain("plaintext without personal data");
        result.Admitted.ShouldBeFalse();
    }
}
