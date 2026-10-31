// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupportStanding;

public class when_classifying_before_walking : a_range_model
{
    [Theory]
    [InlineData(32, 0, 0, false)]
    [InlineData(32, 1, 1, true)]
    [InlineData(33, 0, 0, false)]
    [InlineData(31, 0, 0, false)]
    [InlineData(32, 0, 1, false)]
    public void should_consult_a_throwing_ledger_only_within_range(uint major, uint languageMinor, uint semanticMinor, bool walks)
    {
        Versions(major, languageMinor, major, semanticMinor);
        var ledger = new ThrowingLedger();
        var scope = new SemanticUnknownMemberScope(ImmutableHashSet<SemanticId>.Empty, ImmutableHashSet<SemanticId>.Empty, ImmutableHashSet<string>.Empty);
        var result = EsmSupportStanding.Evaluate(CratisEsmSupport.Descriptor, _model, ledger, scope);
        result.UnknownMembers.IsDefault.ShouldBeFalse();
        result.UnknownMembers.Any().ShouldEqual(walks);
        (ledger.Calls > 0).ShouldEqual(walks);
        result.UnknownMembers.ShouldEachConformTo(member => member.SecurityRelevant && !member.Path.Contains("Never disclose", StringComparison.Ordinal));
    }

    [Fact]
    void should_classify_v31_as_audited_without_walking()
    {
        Versions(31, 0, 31, 0);
        var ledger = new ThrowingLedger();
        var scope = new SemanticUnknownMemberScope(ImmutableHashSet<SemanticId>.Empty, ImmutableHashSet<SemanticId>.Empty, ImmutableHashSet<string>.Empty);
        var result = EsmSupportStanding.Evaluate(CratisEsmSupport.Descriptor, _model, ledger, scope);
        result.Version.ShouldEqual(EsmVersionStanding.Audited);
        ledger.Calls.ShouldEqual(0);
    }

    sealed class ThrowingLedger : IReadOnlyDictionary<string, SemanticSurfaceDisposition>
    {
        internal int Calls { get; private set; }
        public int Count => throw new InvalidOperationException();
        public IEnumerable<string> Keys => throw new InvalidOperationException();
        public IEnumerable<SemanticSurfaceDisposition> Values => throw new InvalidOperationException();
        public SemanticSurfaceDisposition this[string key] => throw new InvalidOperationException();

        public bool ContainsKey(string key)
        {
            Calls++;
            throw new InvalidOperationException("Never disclose this detail");
        }

        public bool TryGetValue(string key, out SemanticSurfaceDisposition value) => throw new InvalidOperationException();
        public IEnumerator<KeyValuePair<string, SemanticSurfaceDisposition>> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
