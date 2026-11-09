// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_a_form_field_picker : given.a_model_path
{
    // A form field's picker is not translated yet - Scene's form field has no place for it - so the field stays a plain input and the model still loads.
    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    public async Task should_load_the_model_with_a_warning_instead_of_refusing_it(string engine)
    {
        const string screen = """
            module Confirmations
              form ReviewForm for Review
                field reviewer
                  choose from query AllReviewers show name
              feature Reviews
                slice StateChange Review
                  command Review
                    id Uuid identifier
                    reviewer Uuid
                    produces ReviewRecorded
                      for id
                      reviewer = reviewer
                  event ReviewRecorded
                    reviewer Uuid
                slice StateView Reviewers
                  readmodel ReviewerRow
                    id Uuid key
                    name String
                  query AllReviewers => ReviewerRow[]
            """;
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n" + screen);
        var issues = new List<StageUnsupportedIssue>();
        LoadedSemanticModel? model = null;
        SceneApplication? scene = null;
        StageApplication? legacy = null;
        var error = await Catch.Exception(async () =>
        {
            if (engine == "semantic") (model, scene) = await SemanticHost.LoadModel(_path, issues);
            else
            {
                legacy = await EventModelHost.LoadModel(_path, issues);
                scene = legacy?.Scene;
            }
        });
        error.ShouldBeNull();
        issues.ShouldBeEmpty();
        scene.ShouldNotBeNull();
        scene!.ConversionDiagnostics.ShouldContain(diagnostic => diagnostic.Code == UiSyntaxAdmission.PickerDiagnosticCode && diagnostic.Path == "reviewer");
    }
}
