// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Reads and writes the line-oriented text of the framework name catalog. Every record is one tab-separated line,
/// the sections come in a fixed order and each section is sorted ordinally, so the same content always produces the
/// same bytes and a regenerated catalog shows its changes as a readable diff.
/// </summary>
internal static class FrameworkNameCatalogText
{
    /// <summary>
    /// The version of the text format.
    /// </summary>
    public const string Format = "1";

    /// <summary>
    /// The command that regenerates the catalog.
    /// </summary>
    public const string RegenerateCommand =
        "STAGE_WRITE_FRAMEWORK_NAME_CATALOG=1 dotnet test Source/Rendering.Cratis/Rendering.Cratis.csproj --filter \"FullyQualifiedName~for_FrameworkNameCatalog\"";

    const string EmptyList = "-";

    /// <summary>
    /// Writes catalog content as text.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <returns>The catalog text, with LF line endings.</returns>
    public static string Write(FrameworkNameCatalogContent content)
    {
        var text = new StringBuilder()
            .Append("# The Cratis C# renderer's framework name catalog. Generated from the scaffold's restored compile assets; do not edit.\n")
            .Append($"# Regenerate: {RegenerateCommand}\n")
            .Append($"format\t{Format}\n")
            .Append($"scaffold\t{content.ScaffoldVersion}\n");
        Section(text, content.Pins.Select(pin => $"pin\t{pin.Key}\t{pin.Value}"));
        Section(text, content.Packages.Select(package => $"package\t{Tag(package.Projects)}\t{package.Id}\t{package.Version}"));
        Section(text, content.Frameworks.Select(framework => $"framework\t{Tag(framework.Projects)}\t{framework.Name}\t{framework.TargetFramework}"));
        Section(text, content.Usings.Select(@using =>
            $"using\t{Tag(@using.Projects)}\t{UsingKind(@using.Kind)}\t{@using.Target}\t{@using.Alias}\t{@using.Source}"));
        Section(text, content.Scopes.Select(scope => $"scope\t{scope}"));
        Section(text, content.Namespaces.Select(@namespace => $"namespace\t{Tag(@namespace.Projects)}\t{@namespace.Name}"));
        Section(text, content.Types.Select(type => $"type\t{Tag(type.Projects)}\t{type.Namespace}\t{type.MetadataName}\t{type.Kind}"));
        Section(text, content.Extensions.Select(extensions =>
            $"extensions\t{Tag(extensions.Projects)}\t{extensions.Namespace}\t{string.Join(' ', extensions.Names)}"));
        Section(text, content.BaseTypes.Select(type =>
            $"base\t{Tag(type.Projects)}\t{type.FullName}\t{type.BaseFullName}\t{List(type.Members)}\t{List(type.NestedTypes)}"));

        return text.ToString();
    }

    /// <summary>
    /// Reads catalog text.
    /// </summary>
    /// <param name="text">The catalog text.</param>
    /// <returns>The catalog content.</returns>
    /// <exception cref="MalformedFrameworkNameCatalog">A line cannot be read.</exception>
    public static FrameworkNameCatalogContent Read(string text)
    {
        var reader = new Reader();
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.Length == 0 || line[0] == '#') continue;
            reader.Read(index + 1, line.Split('\t'));
        }

        return reader.Content(lines.Length);
    }

    static void Section(StringBuilder text, IEnumerable<string> lines)
    {
        foreach (var line in lines.Order(StringComparer.Ordinal))
        {
            text.Append(line).Append('\n');
        }
    }

    // An empty list is written as '-' so that no line ends in whitespace.
    static string List(IEnumerable<string> names) => names.Any() ? string.Join(' ', names) : EmptyList;

    static string Tag(GeneratedProjects projects) => projects switch
    {
        GeneratedProjects.Application => "A",
        GeneratedProjects.Specifications => "S",
        _ => "AS"
    };

    static string UsingKind(CatalogUsingKind kind) => kind switch
    {
        CatalogUsingKind.Static => "static",
        CatalogUsingKind.Alias => "alias",
        _ => "namespace"
    };

    sealed class Reader
    {
        readonly SortedDictionary<string, string> _pins = new(StringComparer.Ordinal);
        readonly List<CatalogPackage> _packages = [];
        readonly List<CatalogFramework> _frameworks = [];
        readonly List<CatalogUsing> _usings = [];
        readonly List<string> _scopes = [];
        readonly List<CatalogNamespace> _namespaces = [];
        readonly List<CatalogedType> _types = [];
        readonly List<CatalogExtensions> _extensions = [];
        readonly List<CatalogBaseType> _baseTypes = [];
        string? _format;
        string? _scaffold;

        public void Read(int line, string[] fields)
        {
            switch (fields[0])
            {
                case "format": _format = Field(line, fields, 1, 2); break;
                case "scaffold": _scaffold = Field(line, fields, 1, 2); break;
                case "pin": Expect(line, fields, 3); _pins[fields[1]] = fields[2]; break;
                case "package": Expect(line, fields, 4); _packages.Add(new(Projects(line, fields[1]), fields[2], fields[3])); break;
                case "framework": Expect(line, fields, 4); _frameworks.Add(new(Projects(line, fields[1]), fields[2], fields[3])); break;
                case "using": Expect(line, fields, 6); _usings.Add(new(Projects(line, fields[1]), Kind(line, fields[2]), fields[3], fields[4], fields[5])); break;
                case "scope": _scopes.Add(Field(line, fields, 1, 2)); break;
                case "namespace": Expect(line, fields, 3); _namespaces.Add(new(Projects(line, fields[1]), fields[2])); break;
                case "type": Expect(line, fields, 5); _types.Add(new(Projects(line, fields[1]), fields[2], fields[3], fields[4])); break;
                case "extensions": Expect(line, fields, 4); _extensions.Add(new(Projects(line, fields[1]), fields[2], Names(fields[3]))); break;
                case "base":
                    Expect(line, fields, 6);
                    _baseTypes.Add(new(Projects(line, fields[1]), fields[2], fields[3], Names(fields[4]), Names(fields[5])));
                    break;
                default: throw new MalformedFrameworkNameCatalog(line, $"unknown record '{fields[0]}'");
            }
        }

        public FrameworkNameCatalogContent Content(int lines)
        {
            if (_format != Format) throw new MalformedFrameworkNameCatalog(lines, $"expected format {Format}, found '{_format}'");
            if (_scaffold is null) throw new MalformedFrameworkNameCatalog(lines, "the scaffold version is missing");

            return new(
                _scaffold,
                _pins.ToImmutableSortedDictionary(StringComparer.Ordinal),
                [.. _packages],
                [.. _frameworks],
                [.. _usings],
                [.. _scopes],
                [.. _namespaces],
                [.. _types],
                [.. _extensions],
                [.. _baseTypes]);
        }

        static ImmutableArray<string> Names(string field) => field == EmptyList ? [] : [.. field.Split(' ', StringSplitOptions.RemoveEmptyEntries)];

        static string Field(int line, string[] fields, int index, int count)
        {
            Expect(line, fields, count);
            return fields[index];
        }

        static void Expect(int line, string[] fields, int count)
        {
            if (fields.Length != count) throw new MalformedFrameworkNameCatalog(line, $"expected {count} fields, found {fields.Length}");
        }

        static GeneratedProjects Projects(int line, string tag) => tag switch
        {
            "A" => GeneratedProjects.Application,
            "S" => GeneratedProjects.Specifications,
            "AS" => GeneratedProjects.Both,
            _ => throw new MalformedFrameworkNameCatalog(line, $"unknown project tag '{tag}'")
        };

        static CatalogUsingKind Kind(int line, string kind) => kind switch
        {
            "namespace" => CatalogUsingKind.Namespace,
            "static" => CatalogUsingKind.Static,
            "alias" => CatalogUsingKind.Alias,
            _ => throw new MalformedFrameworkNameCatalog(line, $"unknown using kind '{kind}'")
        };
    }
}
