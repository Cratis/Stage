// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext;

public class when_inventorying_composite_read_model_keys : Specification
{
    SemanticApplicationContext _context = null!;
    GeneratedTypeName _key = null!;
    IReadOnlyList<GeneratedTypeName> _types = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        CratisRenderingOptions options = new("Payments", "Payments");
        var profile = CratisRendering.CreateProfile(model.Application.Name, options);
        ArtifactRenderRequest request = new(model, SemanticExecutionPlan.Compile(model, []).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, options);
        var readModel = _context.ReadModels.Values.Single();
        _key = new(SliceNaming.Namespace(_context.RootNamespace, _context.DeclaringSlice(readModel.Id).Path), SemanticCompositeKeyAdmission.KeyTypeName(readModel));
    }

    void Because() => _types = _context.GeneratedTypes;

    [Fact] void should_catalog_the_emitted_key_record() => Assert.Contains(_key, _types);
}
