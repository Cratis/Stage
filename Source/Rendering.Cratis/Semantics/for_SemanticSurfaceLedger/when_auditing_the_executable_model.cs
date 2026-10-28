// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

/// <summary>
/// Guards the audited Screenplay semantic surface against drift.
/// </summary>
public class when_auditing_the_executable_model : Specification
{
    IReadOnlyDictionary<string, SemanticSurfaceDisposition> _ledger = null!;
    string[] _missing = [];
    string[] _stale = [];
    string[] _invalid = [];
    int _count;

    void Establish() => _ledger = SemanticSurfaceLedger.Entries;

    void Because()
    {
        var actual = EnumerateSurface().ToHashSet(StringComparer.Ordinal);
        var ledger = _ledger;
        _count = actual.Count;
        _missing = [.. actual.Except(ledger.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        _stale = [.. ledger.Keys.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        _invalid = [.. ledger.Where(entry => entry.Value.Kind switch
            {
                SemanticSurfaceDispositionKind.Rendered => entry.Value.Detail.Length != 0,
                SemanticSurfaceDispositionKind.Rejected => !IsDiagnosticCode(entry.Value.Detail),
                SemanticSurfaceDispositionKind.Ignored => string.IsNullOrWhiteSpace(entry.Value.Detail),
                _ => true
            }).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
    }

    [Fact]
    void should_cover_every_member_without_stale_or_invalid_entries() =>
        Assert.True(_count > 0 && _missing.Length == 0 && _stale.Length == 0 && _invalid.Length == 0,
            $"Audited {_count} semantic members. Missing ({_missing.Length}): {string.Join(", ", _missing)}; " +
            $"Stale ({_stale.Length}): {string.Join(", ", _stale)}; Invalid ({_invalid.Length}): {string.Join(", ", _invalid)}");

    [Theory]
    [InlineData("SemanticEventContract.Visibility")]
    [InlineData("SemanticEventContract.Origin")]
    [InlineData("SemanticEventVisibility.Public")]
    [InlineData("SemanticProjection.Target")]
    [InlineData("SemanticProjectionTargetKind.Event")]
    [InlineData("SemanticSlice.Direction")]
    [InlineData("SemanticTranslationDirection.Inbound")]
    [InlineData("SemanticTranslationDirection.Outbound")]
    [InlineData("SemanticSliceKind.Translate")]
    [InlineData("SemanticCapture.EventsSource")]
    [InlineData("SemanticCaptureEventsSource.Events")]
    [InlineData("SemanticCaptureCondition.Kind")]
    [InlineData("SemanticCaptureConditionKind.Added")]
    [InlineData("SemanticCaptureConditionKind.AnyChanged")]
    [InlineData("SemanticCaptureConditionKind.AllChanged")]
    [InlineData("SemanticCaptureConditionKind.Transition")]
    [InlineData("SemanticCaptureConditionKind.Removed")]
    public void should_classify_translation_members_as_rendered(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Fact] void should_keep_expression_capture_guards_refused() => _ledger["SemanticCaptureConditionKind.Expression"].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-024"));

    [Fact] void should_keep_event_target_reducers_refused() => _ledger["SemanticReducer.Target"].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-032"));

    [Theory]
    [InlineData("SemanticSpecification.WhenAppended")]
    [InlineData("SemanticSpecificationAppend.EventContract")]
    [InlineData("SemanticSpecificationAppend.EventSource")]
    [InlineData("SemanticSpecificationAppend.Values")]
    [InlineData("SemanticSpecificationAppend.Route")]
    [InlineData("SemanticProducedEvent.Route")]
    [InlineData("SemanticReaction.From")]
    [InlineData("SemanticReaction.RunsAs")]
    [InlineData("SemanticReactionIdentity.Kind")]
    [InlineData("SemanticReactionIdentity.Roles")]
    [InlineData("SemanticReactionIdentityKind.System")]
    [InlineData("SemanticReducer.From")]
    [InlineData("SemanticObserverFilter.Source")]
    [InlineData("SemanticObserverFilter.Stream")]
    public void should_classify_supported_identities_and_filters_as_rendered(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Theory]
    [InlineData("SemanticApplication.Triggers")]
    [InlineData("SemanticReactionTrigger.RequirementId")]
    [InlineData("SemanticReactionTriggerKind.Unknown")]
    [InlineData("SemanticReactionTriggerKind.ApplicationTrigger")]
    [InlineData("SemanticReactionTriggerKind.Interval")]
    [InlineData("SemanticReactionTriggerKind.Schedule")]
    [InlineData("SemanticReactionTriggerKind.Startup")]
    [InlineData("SemanticReactionTriggerKind.Shutdown")]
    public void should_keep_every_trigger_kind_and_opaque_body_refused(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-024"));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void should_render_unmixed_reaction_side_effects(bool produces, bool invokes) => SemanticSurfaceLedger.ReactionSideEffectShapes[(produces, invokes)].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Fact] void should_refuse_mixed_reaction_side_effects() => SemanticSurfaceLedger.ReactionSideEffectShapes[(true, true)].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-037"));

    [Theory]
    [InlineData("SemanticSliceKind.Automation")]
    [InlineData("SemanticReactionTriggerKind.Event")]
    [InlineData("SemanticSlice.Reactions")]
    [InlineData("SemanticReactionTrigger.Where")]
    [InlineData("SemanticInvocation.Mappings")]
    public void should_render_event_reactions(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Theory]
    [InlineData("SemanticCommand.Handler")]
    [InlineData("SemanticCommandHandler.RequirementId")]
    [InlineData("SemanticCommandHandler.ResultVersion")]
    [InlineData("SemanticImplementationRequirement.Hints")]
    [InlineData("SemanticImplementationRequirement.IsProvisional")]
    [InlineData("SemanticImplementationRequirement.IsAmbiguous")]
    [InlineData("SemanticAttachmentResolution.Pending")]
    [InlineData("SemanticExecutionCapability.ImplementationBody")]
    public void should_keep_described_implementation_members_refused(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, "STAGE-IMPL-002"));

    [Theory]
    [InlineData("SemanticApplication.ReactionProductionMode")]
    [InlineData("SemanticReactionProductionMode.Atomic")]
    public void should_render_atomic_reaction_batches(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Theory]
    [InlineData("SemanticApplication.CommandReadMode")]
    [InlineData("SemanticCommandReadMode.Protected")]
    [InlineData("SemanticCommand.Reads")]
    [InlineData("SemanticDecisionRead.Name")]
    [InlineData("SemanticDecisionRead.ReadModel")]
    [InlineData("SemanticDecisionRead.Key")]
    [InlineData("SemanticDecisionRead.Presence")]
    [InlineData("SemanticDecisionReadPresence.Required")]
    [InlineData("SemanticConditionOperand.Read")]
    [InlineData("SemanticExpressionKind.ReadPath")]
    [InlineData("SemanticReadPathExpression.Read")]
    [InlineData("SemanticReadPathExpression.PropertyId")]
    public void should_render_protected_command_reads(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Theory]
    [InlineData("SemanticKeyedQuery.KeyParts", "STAGE-ESM-047")]
    [InlineData("SemanticQueryKeyPart.Id", "STAGE-ESM-047")]
    [InlineData("SemanticQueryKeyPart.Name", "STAGE-ESM-047")]
    [InlineData("SemanticQueryKeyPart.Type", "STAGE-ESM-047")]
    [InlineData("SemanticQueryKeyPart.KeyProperty", "STAGE-ESM-047")]
    public void should_keep_key_parts_lookups_refused(string member, string code) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, code));

    [Theory]
    [InlineData("SemanticProjectionPartsKey.Parts")]
    [InlineData("SemanticProjectionKeyKind.Parts")]
    [InlineData("SemanticProjectionKeyPart.Property")]
    [InlineData("SemanticProjectionKeyPart.Value")]
    public void should_render_composite_read_model_keys(string member) => _ledger[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    static bool IsDiagnosticCode(string code) =>
        ((code.StartsWith("STAGE-ESM-", StringComparison.Ordinal) && code.Length == 13) ||
         (code.StartsWith("STAGE-IMPL-", StringComparison.Ordinal) && code.Length == 14)) &&
        code.AsSpan(code.Length - 3).ToString().All(char.IsAsciiDigit);

    static IEnumerable<string> EnumerateSurface()
    {
        var assembly = typeof(ExecutableSemanticModel).Assembly;
        var types = assembly.GetExportedTypes().Where(type => type.Namespace == typeof(SemanticApplication).Namespace).ToArray();

        foreach (var member in EnumerateSupplementarySurface())
        {
            yield return member;
        }

        var pending = new Queue<Type>([typeof(ExecutableSemanticModel), typeof(SemanticApplication)]);
        var visited = new HashSet<Type>();
        while (pending.TryDequeue(out var type))
        {
            type = Unwrap(type);
            if (!types.Contains(type) || !visited.Add(type) || (!type.IsClass && !type.IsValueType))
            {
                continue;
            }

            if (type.IsEnum)
            {
                foreach (var member in Enum.GetNames(type))
                {
                    yield return $"{type.Name}.{member}";
                }

                continue;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (properties.Length == 0)
            {
                // An empty new expression/value variant must still be explicitly classified.
                yield return $"{type.Name}.$type";
            }

            foreach (var property in properties)
            {
                yield return $"{type.Name}.{property.Name}";
                pending.Enqueue(property.PropertyType);
            }

            if (type.IsAbstract)
            {
                foreach (var derived in types.Where(candidate => candidate.IsSubclassOf(type)))
                {
                    pending.Enqueue(derived);
                }
            }
        }
    }

    static IEnumerable<string> EnumerateSupplementarySurface()
    {
        // Compilation exposes implementation attachments beside (not inside) the executable model.
        // Audit their metadata separately without treating compiler source-map types as rendered input.
        foreach (var property in typeof(SemanticImplementationRequirement).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            yield return $"{nameof(SemanticImplementationRequirement)}.{property.Name}";
        }

        // Pending attachment state and the evaluator's body capability are not ESM members,
        // but both must retain an explicit refusal until described bodies are implemented.
        foreach (var member in Enum.GetNames<SemanticAttachmentResolution>())
        {
            yield return $"{nameof(SemanticAttachmentResolution)}.{member}";
        }
        yield return "SemanticExecutionCapability.ImplementationBody";

        // The application exposes these enums through Nullable<T>, outside the ordinary ESM traversal.
        foreach (var member in Enum.GetNames<SemanticCommandReadMode>())
        {
            yield return $"{nameof(SemanticCommandReadMode)}.{member}";
        }
        foreach (var member in Enum.GetNames<SemanticReactionProductionMode>())
        {
            yield return $"{nameof(SemanticReactionProductionMode)}.{member}";
        }
        foreach (var member in Enum.GetNames<SemanticTranslationDirection>())
        {
            yield return $"{nameof(SemanticTranslationDirection)}.{member}";
        }
    }

    static Type Unwrap(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)
        ? type.GetGenericArguments()[0]
        : type;
}
