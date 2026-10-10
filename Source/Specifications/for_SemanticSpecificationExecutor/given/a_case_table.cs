// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_case_table : Specification
{
    protected LoadedSemanticModel _loaded = null!;
    protected SemanticCompilation _compilation = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    string _workspace = null!;

    async Task Establish()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "cratis-stage-specs", "case-tables", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        var path = Path.Combine(_workspace, "Cases.play");
        await File.WriteAllTextAsync(path, """
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId String identifier
                    name String
                    validate
                      name not empty message "Name required"
                    produces ProjectRegistered
                      for projectId
                      name = name
                  event ProjectRegistered
                    name String
                  specification Registering
                    parameter name String
                    parameter expected String
                    case Passing
                      name = ""
                      expected = "Name required"
                    case Failing
                      name = ""
                      expected = "wrong"
                    when RegisterProject
                      projectId = "project-1"
                      name = case.name
                    then error case.expected
                  specification Ordinary
                    when RegisterProject
                      projectId = "project-1"
                      name = ""
                    then error "wrong"
            """);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Cases"));
        var key = Convert.ToHexString(Encoding.UTF8.GetBytes("Cases.play"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Cases.play", await File.ReadAllTextAsync(path));
        _compilation = new SemanticModelCompiler().Compile("Cases", SemanticDocumentSet.Create([document], catalog)).Value!;
    }

    void Destroy() => Directory.Delete(_workspace, recursive: true);
}
