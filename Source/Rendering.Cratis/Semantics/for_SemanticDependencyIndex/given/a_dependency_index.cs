// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.given;

public class a_dependency_index : a_register_project_render_request
{
    internal SemanticDependencyIndex _index = null!;
    protected SemanticApplication _application = null!;
    protected SemanticConcept _projectId = null!;
    protected SemanticConcept _projectName = null!;

    protected static SemanticId Identity(string name) => SemanticId.Create(SemanticKind.Property, name);

    protected static void Replace<T>(T node, string property, object value)
        where T : class
    {
        typeof(T).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(node, value);
    }

    void Establish()
    {
        _application = _model.Application;
        _projectId = _application.Concepts.Single(concept => concept.Name == "ProjectId");
        _projectName = _application.Concepts.Single(concept => concept.Name == "ProjectName");
    }

    protected void BuildIndex()
    {
        var application = _application with
        {
            Modules = [_module with { Features = [_feature with { Slices = [_registerProject, _projectLookup] }] }]
        };

        // Index-only fixtures also exercise refused/future shapes. Do not let model validation preempt the index's incomplete signal.
        var model = _model with { };
        Replace(model, "Application", application);
        _index = new(new SemanticApplicationContext(_request with { Model = model }, _options));
    }
}
