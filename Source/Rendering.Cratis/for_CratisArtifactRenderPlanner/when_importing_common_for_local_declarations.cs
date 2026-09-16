// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_importing_common_for_local_declarations : a_register_project_render_request
{
    string _externalEvent = null!;
    string _localEvent = null!;
    string _primitiveReadModel = null!;
    string _conceptReadModel = null!;
    string _conceptQuery = null!;

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        var commandSlice = context.Slice(_registerProject.Id);
        var command = commandSlice.Slice.Commands.Single();
        var primitiveCommand = command with { Properties = [.. command.Properties.Select(PrimitiveProperty)] };
        var localSlice = commandSlice with { Slice = commandSlice.Slice with { Commands = [primitiveCommand] } };
        _localEvent = SemanticStateChangeArtifactRenderer.Render(localSlice, context).Content;
        _externalEvent = SemanticStateChangeArtifactRenderer.Render(localSlice with { Slice = localSlice.Slice with { Events = [] } }, context).Content;

        // Render fragments to isolate declaration imports. Admission is intentionally not broadened to allow
        // primitive projection identities: an emitted query and its behavior remain exactly as before.
        var viewSlice = context.Slice(_projectLookup.Id);
        var readModel = viewSlice.Slice.ReadModels.Single();
        var primitiveView = viewSlice with
        {
            Slice = viewSlice.Slice with
            {
                ReadModels = [readModel with { Properties = [.. readModel.Properties.Select(PrimitiveProperty)] }],
                Queries = []
            }
        };
        _primitiveReadModel = SemanticStateViewArtifactRenderer.Render(primitiveView, context).Content;
        _conceptReadModel = SemanticStateViewArtifactRenderer.Render(viewSlice with { Slice = viewSlice.Slice with { Queries = [] } }, context).Content;
        _conceptQuery = SemanticStateViewArtifactRenderer.Render(primitiveView with { Slice = primitiveView.Slice with { Queries = viewSlice.Slice.Queries } }, context).Content;
    }

    [Fact] void should_import_common_for_a_locally_declared_event_signature() => _localEvent.ShouldContain("using Projects.Common;");
    [Fact] void should_not_import_common_for_an_externally_declared_event_signature() => _externalEvent.ShouldNotContain("using Projects.Common;");
    [Fact] void should_still_return_the_external_event() => _externalEvent.ShouldContain("public ProjectRegistered Handle() => new(ProjectId, Name);");
    [Fact] void should_not_import_common_for_primitive_read_model_properties() => _primitiveReadModel.ShouldNotContain("using Projects.Common;");
    [Fact] void should_import_common_for_concept_read_model_properties() => _conceptReadModel.ShouldContain("using Projects.Common;");
    [Fact] void should_import_common_for_an_emitted_concept_query_argument() => _conceptQuery.ShouldContain("using Projects.Common;");
    [Fact] void should_preserve_the_existing_query_expression() => _conceptQuery.ShouldContain("await readModels.GetInstanceById<ProjectSummary>((EventSourceId)projectId)");

    SemanticProperty PrimitiveProperty(SemanticProperty property) => property with
    {
        Type = SemanticTypeReference.ForPrimitive(_model.Application.Concepts.Single(_ => _.Id == property.Type.Target).Primitive)
    };
}
