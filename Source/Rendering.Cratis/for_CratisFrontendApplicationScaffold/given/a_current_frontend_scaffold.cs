// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;

public class a_current_frontend_scaffold : Specification
{
    protected const string InputNamePrefix = "cratis-scaffold:text:";
    protected CratisBackendApplicationScaffoldProfile _profile = null!;
    protected CratisBackendApplicationScaffoldRequest _request = null!;
    protected ImmutableArray<ArtifactRenderInput> _first;
    protected ImmutableArray<ArtifactRenderInput> _second;

    void Establish()
    {
        _profile = CratisBackendApplicationScaffoldProfile.Current;
        _request = CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp", "MyApp", _profile);
        var scaffold = new CratisFrontendApplicationScaffold();
        _first = scaffold.Create(_request);
        _second = scaffold.Create(_request);
    }

    protected static string PathOf(ArtifactRenderInput input) => input.Name[InputNamePrefix.Length..];

    protected static string Text(ArtifactRenderInput input) => new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan());

    protected string Content(string path) => Text(_first.Single(input => PathOf(input) == path));
}
