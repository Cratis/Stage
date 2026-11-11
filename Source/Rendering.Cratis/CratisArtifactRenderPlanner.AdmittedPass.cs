// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Realization;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs;
using Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis;

public sealed partial class CratisArtifactRenderPlanner
{
    /// <summary>
    /// One admitted planning pass; its steps run in a fixed order because artifact and diagnostic ordering is part of the plan.
    /// </summary>
    sealed class AdmittedPass
    {
        readonly ArtifactRenderRequest _request;
        readonly CratisRenderingOptions _options;
        readonly OwnerOmissionAttempt? _attempt;
        readonly List<ArtifactRenderDiagnostic> _diagnostics;
        readonly List<PlannedArtifact> _artifacts = [];
        SemanticApplicationContext _context = null!;
        SemanticOwnerOmission _omission = SemanticOwnerOmission.None;
        IReadOnlyList<LocatedSemanticSlice> _slices = [];
        ImmutableArray<SemanticTypedContextDescriptor> _descriptors;
        ImmutableArray<LifecycleRefusal> _lifecycleRefusals;
        ImmutableArray<ArtifactRemoval> _removals;
        ImmutableArray<ArtifactRenderRefusal> _refusals;
        ScreenPartitionsInput.Catalog? _partitions;
        IReadOnlyList<LocatedSemanticSlice>? _userInterface;
        string? _sceneJson;
        bool _reducersSelected;

        internal AdmittedPass(ArtifactRenderRequest request, CratisRenderingOptions options, List<ArtifactRenderDiagnostic> diagnostics, OwnerOmissionAttempt? attempt)
        {
            _request = request;
            _options = options;
            _diagnostics = diagnostics;
            _attempt = attempt;
        }

        internal OwnerOmissionFailure Failure => new(_context, _diagnostics, _removals);

        internal bool Failed => PassFailed(_diagnostics, _refusals, _attempt);

        internal void Admit()
        {
            var context = new SemanticApplicationContext(_request, _options);
            context.ScopeFlatProjections();
            context.DegradeEventContextMappings([]);
            context.DenyUnsupportedValidations([]);
            _diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, context.SelectedSlices()));
            _diagnostics.AddRange(SemanticImplementationAdmission.Verify(_request));
            _diagnostics.AddRange(SemanticTypedContextAdmission.Verify(_request, out _descriptors));
            _diagnostics.AddRange(SemanticEventLineageAdmission.Verify(_request));

            _context = AdmittedContext();
            _omission = OmissionFor(_attempt);
            _context.OmittedOwners = _omission.Closure;
            _slices = _omission.Apply(_context.SelectedSlices());
            SemanticCratisAdmission.Evaluate(_context, _slices, _diagnostics);
            _lifecycleRefusals = SemanticLifecycleAdmission.Check(_context);
            _diagnostics.AddRange(_lifecycleRefusals.Select(refusal => Error(refusal.Code, refusal.Message, _context.Application.Id)));
            _diagnostics.AddRange(GraphDiagnostics(_request, _context));
            _removals = SemanticCratisAdmission.AdmitReducerMigration(_context, _diagnostics);
        }

        internal void RenderDescriptors()
        {
            var selectedReducers = _omission.Apply(SemanticReducerTypedContexts.Selected(_context, _slices))
                .Select(descriptor => (descriptor.RequirementId, descriptor.OperationId!.Value)).ToHashSet();
            _reducersSelected = selectedReducers.Count > 0;
            foreach (var descriptor in _omission.Apply(_descriptors))
            {
                RenderDescriptor(descriptor, selectedReducers);
            }
        }

        internal void PrepareInterface()
        {
            ScreenPartitionsInput.TryRead(_request.Profile, _request.Model, out _partitions, out _);
            _userInterface = UserInterfaceSlices(_request, _context, _slices, _diagnostics) is { } interfaceSlices ? _omission.Apply(interfaceSlices) : null;
            _sceneJson = AdmittedScene(_request, _partitions, _userInterface, _diagnostics);
            var evidence = OmissionEvidence(_attempt);
            _diagnostics.AddRange(evidence.Warnings);
            _refusals = SemanticRefusalScope.Degrade(_context, _diagnostics);
            if (!Failed) _refusals = _refusals.AddRange(evidence.Refusals);
        }

        internal void Refuse() => _context.RefuseSpecifications([.. _refusals.Select(refusal => refusal.Artifact)]);

        internal void RenderRuntime()
        {
            foreach (var descriptor in _omission.Apply(SemanticValidationContextRuntime.Selected(_context, _slices)))
            {
                _artifacts.Add(Artifact(SemanticValidationContextRuntime.RenderWrapper(_context, descriptor)));
            }

            if (SemanticValidationContextRuntime.UsesCommandReceiptTime(_context, _slices))
            {
                _artifacts.Add(Artifact(CommandReceiptTimeRendering.Render(_context.RootNamespace, _context.GeneratedTypes)));
            }

            if (GeneratedTypeNames.UsesProtectedReads(_slices.SelectMany(located => located.Slice.Commands)))
            {
                _artifacts.Add(Artifact(SemanticProtectedReadAdmissionRenderer.Render(_context, _slices.SelectMany(located => located.Slice.Commands))));
            }

            if (GeneratedTypeNames.UsesStreamIds(_slices.Select(slice => slice.Slice)))
            {
                _artifacts.Add(Artifact(StreamIdsRendering.Render(_context.RootNamespace, _context.GeneratedTypes)));
            }

            if (_reducersSelected)
            {
                _artifacts.Add(Artifact(SemanticReducerContextRuntime.Render(_context)));
                _artifacts.Add(Artifact(TenantTranslationSource.Render(_context.RootNamespace, _context.GeneratedTypes)));
            }

            AddScaffold(_request, _context, _artifacts);
            if (_request.Scope.Kind == ArtifactRenderScopeKind.Application && ApplicationRealizationInput.From(_request.Profile) is { } realization)
            {
                _diagnostics.AddRange(RealizationDiagnostics.For(realization, _context.Application.Id, SignedForwardedIdentityInput.IsCarriedBy(_request.Profile)));
            }
        }

        internal void RenderInterface()
        {
            if (_partitions is not null)
            {
                _artifacts.AddRange(ScreenPartitionsInput.Render(_partitions, _slices));
                AddPartitionNotes(_partitions);
            }

            if (_userInterface is not null)
            {
                _artifacts.AddRange(UserInterfaceArtifacts(_request, _context, _userInterface, _sceneJson));
            }
        }

        internal void RenderSemantics()
        {
            RenderCommon();
            _artifacts.AddRange(SemanticEventSourceArtifactRenderer.Selected(_context, _slices).Select(source => Artifact(SemanticEventSourceArtifactRenderer.Render(source, _context))));
            RenderPolicies();
            foreach (var located in _slices)
            {
                var renderer = SemanticSliceArtifactRenderers.Ordered.FirstOrDefault(_ => _.Handles(located)) ??
                    throw UnsupportedSemanticRendering.For(nameof(SemanticSliceKind), located.Slice.Kind);
                _artifacts.AddRange(renderer.Render(located, _context).Select(Artifact));
                _artifacts.AddRange(SemanticEventContractsArtifactRenderer.Render(located, _context).Select(Artifact));
            }
        }

        internal void RenderApplicationScope()
        {
            if (_lifecycleRefusals.IsEmpty && _request.Scope.Kind == ArtifactRenderScopeKind.Application)
            {
                _artifacts.AddRange(SemanticLifecycleArtifactRenderer.Render(_context).Select(Artifact));
                _diagnostics.AddRange(_context.Application.StateMachines.Select(machine => new ArtifactRenderDiagnostic(
                    LifecycleCodes.RestartState,
                    ArtifactRenderDiagnosticSeverity.Warning,
                    $"State machine '{machine.Name}' resumes in its initial state after its grain is reactivated: Cratis.Choreography resolves the state of an event-sourced machine from the followed read model alone, and '{machine.Name}' declares no read model member that records its state.",
                    _context.Application.Id)));
            }

            if (_request.Scope.Kind == ArtifactRenderScopeKind.Application && GraphsRender()) RenderGraphs();
        }

        internal ArtifactRenderPlan BuildPlan()
        {
            _artifacts.AddRange(SemanticCratisAdmission.SelectedConstraints(_context, _slices)
                .Select(_ => Artifact(SemanticConstraintArtifactRenderer.Render(_.Slice, _.Constraint, _context))));
            _diagnostics.AddRange(_context.InlineDiagnostics);
            var plan = _context.WithInlineInputs(CreatePlan(_request, _artifacts, _diagnostics, new(_refusals, _removals)));
            return _context.Degradations.Count > 0 ? plan.WithAcceptance([.. plan.Degradations, .. _context.Degradations]) : plan;
        }

        // Dependent body checks must not dereference rejected sidecar entries. Keep their refusals,
        // but expose only unique requirements and admitted descriptors to those checks.
        SemanticApplicationContext AdmittedContext()
        {
            var admittedRequest = _request with
            {
                ImplementationRequirements = _request.ImplementationRequirements.IsDefault ? [] :
                    [.. _request.ImplementationRequirements.Where(requirement => requirement is not null)
                        .GroupBy(requirement => requirement.RequirementId, StringComparer.Ordinal)
                        .Where(group => group.Count() == 1).Select(group => group.Single())],
                TypedContextDescriptors = _descriptors
            };
            var context = new SemanticApplicationContext(admittedRequest, _options);
            context.ScopeFlatProjections();
            context.DegradeEventContextMappings(_diagnostics);
            context.DenyUnsupportedValidations(_diagnostics);
            return context;
        }

        void RenderDescriptor(SemanticTypedContextDescriptor descriptor, HashSet<(string RequirementId, SemanticId Operation)> selectedReducers)
        {
            try
            {
                var wrapper = SemanticCommandBodyAdmission.RenderDescriptor(descriptor, _context);
                if (descriptor.OperationId is { } operation && selectedReducers.Contains((descriptor.RequirementId, operation)))
                {
                    _artifacts.Add(Artifact(wrapper));
                }
            }
            catch (InvalidTypedContext exception)
            {
                _diagnostics.Add(Error("STAGE-ESM-021", exception.Message, descriptor.OperationId!.Value));
            }
            catch (UnsupportedSemanticRendering exception)
            {
                _diagnostics.Add(Error(exception.Code, exception.Message, descriptor.OperationId!.Value));
            }
        }

        void AddPartitionNotes(ScreenPartitionsInput.Catalog partitions)
        {
            foreach (var slice in _slices.Where(slice => !partitions.Entries.Exists(entry => entry.Owner == slice.Slice.Id.ToString())))
            {
                _diagnostics.Add(new("STAGE-SCREEN-005", ArtifactRenderDiagnosticSeverity.Information, "This slice declares no authored screen; no slice-owned default UI fragment is emitted.", slice.Slice.Id));
            }

            if (_userInterface is null)
            {
                _diagnostics.Add(new("STAGE-SCREEN-006", ArtifactRenderDiagnosticSeverity.Information, "Application Scene aggregates are omitted from scoped plans; use application scope, or name the slices whose UI is present, to publish shared UI.", _context.Application.Id));
            }
        }

        void RenderCommon()
        {
            var shared = _request.Scope.Kind == ArtifactRenderScopeKind.Application
                ? (_context.Application.Concepts, _context.Application.Types)
                : SemanticSharedReferences.Collect(_context, _slices);
            if (_context.EventContracts.Any)
            {
                AddEventContractCommon(_context, shared, _artifacts, _diagnostics);
                return;
            }

            _artifacts.AddRange(shared.Concepts.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, _context))));
            _artifacts.AddRange(shared.Types.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, _context))));
        }

        void RenderPolicies()
        {
            if (SemanticPolicyNames.Operations(_slices, _context.Application.Policies).Count == 0 && !SemanticQueryNarrowingRendering.Narrowed(_context, _slices).Any()) return;

            _artifacts.AddRange(SemanticPolicyArtifactRenderer.Render(_context, _slices).Select(Artifact));
            _artifacts.AddRange(SemanticPolicyArtifactRenderer.RenderOpaque(_context, _slices).Select(Artifact));
            _artifacts.AddRange(SemanticQueryNarrowingRendering.Render(_context, _slices).Select(Artifact));
        }

        bool GraphsRender() => SemanticGraphAdmission.Declares(_context.Application) && SemanticGraphAdmission.Check(_context, ChronicleGraphSupport.PinnedClientAdmits).IsEmpty;

        void RenderGraphs()
        {
            _artifacts.AddRange(SemanticGraphArtifactRenderer.Render(_context)
                .Concat(_context.Application.GraphSpecifications.Where(specification => !SemanticGraphAdmission.IsSeeded(specification)).Select(specification => SemanticGraphSpecificationRenderer.Render(specification, _context)))
                .Select(Artifact));
            var omitted = SemanticGraphAdmission.Omitted(_context);
            _diagnostics.AddRange(omitted.Select(degradation => degradation.Diagnostic));
            _context.Degradations.AddRange(omitted);
        }
    }
}
