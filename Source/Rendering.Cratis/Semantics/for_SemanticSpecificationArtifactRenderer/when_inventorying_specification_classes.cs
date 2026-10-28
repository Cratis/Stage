// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer;

public class when_inventorying_specification_classes
{
    readonly List<string> _mismatches = [];
    int _compared;

    public when_inventorying_specification_classes()
    {
        foreach (var (name, request, options) in inventory_fixtures.All())
        {
            var context = inventory_fixtures.Admitted(request, options);
            var inventory = context.GeneratedTypes.Select(type => CatalogNames.Qualify(type.Namespace, type.Name)).ToHashSet(StringComparer.Ordinal);
            foreach (var located in context.SelectedSlices())
            {
                foreach (var specification in located.Slice.Specifications.Where(specification => !context.RefusedSpecifications.Contains(specification.Id)))
                {
                    Compare(name, specification, located, context, inventory);
                }
            }
        }
    }

    [Fact]
    void should_inventory_exactly_the_classes_each_specification_renders()
    {
        Assert.True(_compared > 0, "No specification was compared.");
        Assert.True(_mismatches.Count == 0, string.Join('\n', _mismatches));
    }

    void Compare(string fixture, Screenplay.Semantics.SemanticSpecification specification, LocatedSemanticSlice located, SemanticApplicationContext context, HashSet<string> inventory)
    {
        var emitted = SemanticSpecificationArtifactRenderer.Render(specification, context)
            .SelectMany(file => CSharpSyntaxTree.ParseText(file.Content).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(type => type.BaseList?.Types.Any(baseType => baseType.Type.ToString().EndsWith("Specification", StringComparison.Ordinal)) == true)
                .Select(type => $"{type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().First().Name}.{type.Identifier.Text}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var declared = SemanticCratisAdmission.GeneratedSpecificationDeclarations(context, [located])
            .Where(declaration => declaration.Artifact == specification.Id)
            .Select(declaration => $"{CatalogNames.Qualify(context.RootNamespace, declaration.Namespace)}.{declaration.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        _compared++;
        if (!emitted.SequenceEqual(declared, StringComparer.Ordinal))
        {
            _mismatches.Add($"{fixture}/{specification.Name}: emitted [{string.Join(", ", emitted)}] but inventoried [{string.Join(", ", declared)}]");
        }
        foreach (var name in emitted.Where(name => !inventory.Contains(name)))
        {
            _mismatches.Add($"{fixture}/{specification.Name}: {name} is not in the generated-type inventory");
        }
    }
}
#endif
