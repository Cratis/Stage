// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

internal static class SpecificationRefusals
{
    internal static ImmutableArray<RefusedSpecification> Selected(LoadedSemanticModel loaded, IReadOnlyCollection<SemanticId> scopes) =>
        new Selection(loaded, scopes).Run();

    sealed class Selection(LoadedSemanticModel loaded, IReadOnlyCollection<SemanticId> scopes)
    {
        readonly HashSet<SemanticId> _selected = [.. scopes];

        internal ImmutableArray<RefusedSpecification> Run()
        {
            if (scopes.Count == 0 || scopes.Contains(loaded.Model.Application.Id)) return loaded.RefusedSpecifications;
            foreach (var module in loaded.Model.Application.Modules)
            {
                SelectFeatures(module.Name, module.Features, [], scopes.Contains(module.Id));
            }
            return [.. loaded.RefusedSpecifications.Where(refusal => _selected.Contains(refusal.Id))];
        }

        void SelectFeatures(string module, IEnumerable<SemanticFeature> features, string[] parents, bool inherited)
        {
            foreach (var feature in features)
            {
                string[] path = [.. parents, feature.Name];
                var included = inherited || scopes.Contains(feature.Id);
                foreach (var slice in feature.Slices.Where(slice => included || scopes.Contains(slice.Id)))
                {
                    SelectSlice(SemanticAddress.ForSlice(loaded.IdentityCatalog!.Application, module, [.. path], slice.Name));
                }
                SelectFeatures(module, feature.Features, path, included);
            }
        }

        void SelectSlice(SemanticAddress slice)
        {
            foreach (var refusal in loaded.RefusedSpecifications)
            {
                if (refusal.Origin is not { } origin) continue;
                var address = SemanticAddress.ForSpecification(slice, origin.Effective.Name);
                if (loaded.IdentityCatalog?.ResolveSemantic(address) == refusal.Id) _selected.Add(refusal.Id);
            }
            foreach (var assignment in loaded.SemanticIdentities.Where(assignment => assignment.Address.Parts.Take(slice.Parts.Length).SequenceEqual(slice.Parts)))
            {
                _selected.Add(assignment.Id);
            }
        }
    }
}
