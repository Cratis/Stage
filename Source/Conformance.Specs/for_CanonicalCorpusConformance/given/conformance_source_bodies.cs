// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Compiles a vector's source form, so the renderer is planned with the implementation requirements, resolved bodies and typed contexts the
// source carries, as the three-way specs plan it.
public static class conformance_source_bodies
{
    // Loaded is null when the vector has no source form; Problem is set when the source cannot stand in for the ESM bytes.
    public static (LoadedSemanticModel? Loaded, string? Problem) Compile(conformance_vector vector)
    {
        var form = vector.Corpus?.SourceForms.FirstOrDefault(value => value.Name == "single");
        if (form is null)
        {
            return (null, null);
        }

        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compiled = new SemanticModelCompiler().Compile(vector.Corpus!.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        if (!compiled.Success)
        {
            return (null, "source-form-does-not-compile");
        }

        var model = compiled.Value!.Model;

        // A frozen vector whose source now binds to a later semantic version (PublicEventsCorpus.V9 since typed capture identities in
        // V32) keeps its ESM bytes on purpose; the bytes alone are then the vector, without source bodies.
        if (vector.Name == "PublicEventsCorpus.V9" && model.Revision != vector.Model.Revision && model.SemanticVersion.Major > vector.Model.SemanticVersion.Major)
        {
            return (null, null);
        }

        if (model.Revision != vector.Model.Revision)
        {
            return (null, "source-form-differs-from-esm-bytes");
        }

        // Match the partial-plan policy used by the loader and each execution surface.
        if (SemanticExecutionPlan.Compile(model, []).Plan is not { } plan)
        {
            return (null, null);
        }

        return (new LoadedSemanticModel(model, plan)
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(compiled.Value.Documents, compiled.ImplementationRequirements),
            TypedContextDescriptors = compiled.TypedContextDescriptors
        }, null);
    }
}
#endif
