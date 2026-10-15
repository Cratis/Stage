// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver.given;

/// <summary>
/// A small catalog with the shapes the real one has: global usings, a namespace the application imports, framework
/// types sharing a name across namespaces, an attribute, extension members and a namespace whose types are not recorded.
/// </summary>
public class a_framework_catalog : Specification
{
    protected const string FileNamespace = "Shop.Orders.Placing";
    private protected FrameworkNameCatalog _catalog = null!;
    private protected ImmutableArray<GeneratedTypeName> _generated = [];

    void Establish()
    {
        _catalog = new(new(
            "2",
            ImmutableSortedDictionary<string, string>.Empty,
            [],
            [],
            [
                new(GeneratedProjects.Both, CatalogUsingKind.Namespace, "System", string.Empty, "sdk"),
                new(GeneratedProjects.Both, CatalogUsingKind.Namespace, "System.Linq", string.Empty, "sdk"),
                new(GeneratedProjects.Both, CatalogUsingKind.Namespace, "Cratis.Arc.Identity", string.Empty, "cratis"),
                new(GeneratedProjects.Specifications, CatalogUsingKind.Namespace, "Xunit", string.Empty, "xunit")
            ],
            ["Cratis", "System", "Xunit"],
            [.. new[] { "System", "System.Linq", "System.Text", "System.Text.RegularExpressions", "Cratis", "Cratis.Arc", "Cratis.Arc.Identity", "Cratis.Chronicle",
                "Cratis.Chronicle.Identities", "Cratis.Chronicle.Keys", "Cratis.Chronicle.Testing", "Xunit", "Unrecorded" }
                .Select(name => new CatalogNamespace(GeneratedProjects.Both, name))],
            [
                Type("System", "Guid", "struct"),
                Type("System", "String"),
                Type("System", "Math", "static"),
                Type("System.Linq", "Enumerable", "static"),
                Type("System.Text.RegularExpressions", "Regex"),
                Type("System.Text.RegularExpressions", "Match"),
                Type("System.Text.RegularExpressions", "RegexOptions", "enum"),
                Type("Cratis.Arc.Identity", "IIdentityProvider", "interface"),
                Type("Cratis.Chronicle.Identities", "IIdentityProvider", "interface"),
                Type("Cratis.Chronicle.Identities", "Identity"),
                Type("Cratis.Chronicle.Keys", "KeyAttribute", "attribute"),
                Type("Cratis.Chronicle.Keys", "KeyType", "enum"),
                Type("Cratis.Chronicle.Testing", "EventScenario`1"),
                Type("Cratis.Chronicle.Testing", "ScenarioExtensions", "static"),
                Type("Xunit", "FactAttribute", "attribute")
            ],
            [
                new(GeneratedProjects.Both, "System.Linq", ["Select", "Where"]),
                new(GeneratedProjects.Both, "Cratis.Chronicle.Testing", ["ShouldHaveAppended"])
            ],
            []));
        _generated =
        [
            new(FileNamespace, "PlaceOrder"),
            new(FileNamespace, "OrderPlaced"),
            new("Shop.Common", "OrderId"),
            new("Shop.Orders.Shipping", "Shipment")
        ];
    }

    private protected ImportRequest Request(params TypeReference[] references) => new(FileNamespace, GeneratedProjects.Application, _generated, references);

    static CatalogedType Type(string @namespace, string metadataName, string kind = "class") =>
        new(GeneratedProjects.Both, @namespace, metadataName, kind);
}
