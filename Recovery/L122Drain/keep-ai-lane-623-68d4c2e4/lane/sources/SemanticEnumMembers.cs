1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using System.Globalization;
5: using Cratis.Screenplay.Semantics;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: 
8: namespace Cratis.Stage.Rendering.Cratis.Semantics;
9: 
10: /// <summary>
11: /// Preserves model-owned enum numbers in generated and analysis declarations.
12: /// </summary>
13: internal static class SemanticEnumMembers
14: {
15:     internal static IEnumerable<string> Declarations(SemanticConcept concept) =>
16:         Declarations(concept.Values, concept.Numbers is { } numbers ? numbers : null);
17: 
18:     internal static IEnumerable<string> Declarations(IEnumerable<string> values, IEnumerable<int>? numbers)
19:     {
20:         var identities = numbers?.ToArray();
21: 
22:         return values.Select((value, index) => identities is not null
23:             ? $"{Identifiers.ToPascalCase(value)} = {identities[index].ToString(CultureInfo.InvariantCulture)}"
24:             : Identifiers.ToPascalCase(value));
25:     }
26: }
