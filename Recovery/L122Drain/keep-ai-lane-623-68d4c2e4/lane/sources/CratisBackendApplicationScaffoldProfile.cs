1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: namespace Cratis.Stage.Rendering.Cratis.Scaffolding;
5: 
6: /// <summary>
7: /// Represents the exact version profile used to scaffold a first-run Cratis backend application.
8: /// </summary>
9: public sealed class CratisBackendApplicationScaffoldProfile
10: {
11:     const string CurrentVersion = "2";
12: 
13:     CratisBackendApplicationScaffoldProfile(
14:         string version,
15:         string targetFramework,
16:         string cratisPackageVersion,
17:         string cratisArcMongoDBPackageVersion,
18:         string cratisArcChronicleTestingPackageVersion,
19:         string cratisSpecificationsPackageVersion,
20:         string cratisSpecificationsXUnitPackageVersion,
21:         string microsoftNetTestSdkPackageVersion,
22:         string nSubstitutePackageVersion,
23:         string xunitPackageVersion,
24:         string xunitRunnerVisualStudioPackageVersion,
25:         string chronicleImageVersion,
26:         CratisFrontendPackageSet frontendPackageSet)
27:     {
28:         Version = version;
29:         TargetFramework = targetFramework;
30:         CratisPackageVersion = cratisPackageVersion;
31:         CratisArcMongoDBPackageVersion = cratisArcMongoDBPackageVersion;
32:         CratisArcChronicleTestingPackageVersion = cratisArcChronicleTestingPackageVersion;
33:         CratisSpecificationsPackageVersion = cratisSpecificationsPackageVersion;
34:         CratisSpecificationsXUnitPackageVersion = cratisSpecificationsXUnitPackageVersion;
35:         MicrosoftNetTestSdkPackageVersion = microsoftNetTestSdkPackageVersion;
36:         NSubstitutePackageVersion = nSubstitutePackageVersion;
37:         XunitPackageVersion = xunitPackageVersion;
38:         XunitRunnerVisualStudioPackageVersion = xunitRunnerVisualStudioPackageVersion;
39:         ChronicleImageVersion = chronicleImageVersion;
40:         FrontendPackageSet = frontendPackageSet;
41:     }
42: 
43:     /// <summary>
44:     /// Gets the profile Stage currently supports and verifies.
45:     /// </summary>
46:     /// <remarks>
47:     /// <para>
48:     /// A generated application is pinned to the current Cratis release, so someone generating one starts where
49:     /// the framework is rather than where it was when this profile was last touched. Every version here is an
50:     /// exact release, and moving them is a deliberate, reviewed change: the emitted bytes are frozen by
51:     /// specification, so a bump cannot happen silently.
52:     /// </para>
53:     /// <para>
54:     /// The image and the explicitly pinned Chronicle client must match. The Cratis metapackage can depend on an
55:     /// older Chronicle client than the current kernel; relying on that transitive dependency would silently drift
56:     /// from the image. Running a newer client against an older kernel can fail the descriptor handshake outright.
57:     /// </para>
58:     /// </remarks>
59:     public static CratisBackendApplicationScaffoldProfile Current { get; } = Create(
60:         CurrentVersion,
61:         "net10.0",
62:         "22.78.0",
63:         "22.78.0",
64:         "22.78.0",
65:         "4.1.1",
66:         "4.1.1",
67:         "18.10.1",
68:         "6.2.0",
69:         "2.9.3",
70:         "4.0.0",
71:         "19.45.0");
72: 
73:     /// <summary>
74:     /// Gets the scaffold contract version carried by every generated input.
75:     /// </summary>
76:     public string Version { get; }
77: 
78:     /// <summary>
79:     /// Gets the exact target framework moniker.
80:     /// </summary>
81:     public string TargetFramework { get; }
82: 
83:     /// <summary>
84:     /// Gets the exact <c language="csharp">Cratis</c> package version.
85:     /// </summary>
86:     public string CratisPackageVersion { get; }
87: 
88:     /// <summary>
89:     /// Gets the exact <c language="csharp">Cratis.Arc.MongoDB</c> package version.
90:     /// </summary>
91:     public string CratisArcMongoDBPackageVersion { get; }
92: 
93:     /// <summary>
94:     /// Gets the exact <c language="csharp">Cratis.Arc.Chronicle.Testing</c> package version.
95:     /// </summary>
96:     public string CratisArcChronicleTestingPackageVersion { get; }
97: 
98:     /// <summary>
99:     /// Gets the exact <c language="csharp">Cratis.Specifications</c> package version.
100:     /// </summary>
101:     public string CratisSpecificationsPackageVersion { get; }
102: 
103:     /// <summary>
104:     /// Gets the exact <c language="csharp">Cratis.Specifications.XUnit</c> package version.
105:     /// </summary>
106:     public string CratisSpecificationsXUnitPackageVersion { get; }
107: 
108:     /// <summary>
109:     /// Gets the exact <c language="csharp">Microsoft.NET.Test.Sdk</c> package version.
110:     /// </summary>
111:     public string MicrosoftNetTestSdkPackageVersion { get; }
112: 
113:     /// <summary>
114:     /// Gets the exact <c language="csharp">NSubstitute</c> package version.
115:     /// </summary>
116:     public string NSubstitutePackageVersion { get; }
117: 
118:     /// <summary>
119:     /// Gets the exact <c language="csharp">xunit</c> package version.
120:     /// </summary>
121:     public string XunitPackageVersion { get; }
122: 
123:     /// <summary>
124:     /// Gets the exact <c language="csharp">xunit.runner.visualstudio</c> package version.
125:     /// </summary>
126:     public string XunitRunnerVisualStudioPackageVersion { get; }
127: 
128:     /// <summary>
129:     /// Gets the exact <c language="csharp">cratis/chronicle</c> image version.
130:     /// </summary>
131:     public string ChronicleImageVersion { get; }
132: 
133:     /// <summary>
134:     /// Gets the explicitly pinned Chronicle client, ASP.NET Core integration and testing version matching the kernel image.
135:     /// </summary>
136:     public string CratisChroniclePackageVersion => ChronicleImageVersion;
137: 
138:     /// <summary>
139:     /// Gets the exact npm package versions the emitted era composes its frontend from.
140:     /// </summary>
141:     /// <remarks>
142:     /// Held as a separate <see cref="CratisFrontendPackageSet"/> rather than as more version properties here, because
143:     /// the frontend packages resolve from npm on their own publish cadence while the properties above resolve from
144:     /// NuGet, and because neither is the same thing as the <b>tooling</b> versions the Stage repository itself builds
145:     /// with. All of them are validated by the one exactness rule this type owns, so no emitted era can carry a
146:     /// wildcard, a range or a prerelease from either registry.
147:     /// </remarks>
148:     public CratisFrontendPackageSet FrontendPackageSet { get; }
149: 
150:     /// <summary>
151:     /// Creates a validated immutable scaffold profile for in-assembly contract verification.
152:     /// </summary>
153:     /// <param name="version">The scaffold contract version.</param>
154:     /// <param name="targetFramework">The exact target framework moniker.</param>
155:     /// <param name="cratisPackageVersion">The exact <c language="csharp">Cratis</c> package version.</param>
156:     /// <param name="cratisArcMongoDBPackageVersion">The exact <c language="csharp">Cratis.Arc.MongoDB</c> package version.</param>
157:     /// <param name="cratisArcChronicleTestingPackageVersion">The exact <c language="csharp">Cratis.Arc.Chronicle.Testing</c> package version.</param>
158:     /// <param name="cratisSpecificationsPackageVersion">The exact <c language="csharp">Cratis.Specifications</c> package version.</param>
159:     /// <param name="cratisSpecificationsXUnitPackageVersion">The exact <c language="csharp">Cratis.Specifications.XUnit</c> package version.</param>
160:     /// <param name="microsoftNetTestSdkPackageVersion">The exact <c language="csharp">Microsoft.NET.Test.Sdk</c> package version.</param>
161:     /// <param name="nSubstitutePackageVersion">The exact <c language="csharp">NSubstitute</c> package version.</param>
162:     /// <param name="xunitPackageVersion">The exact <c language="csharp">xunit</c> package version.</param>
163:     /// <param name="xunitRunnerVisualStudioPackageVersion">The exact <c language="csharp">xunit.runner.visualstudio</c> package version.</param>
164:     /// <param name="chronicleImageVersion">The exact <c language="csharp">cratis/chronicle</c> image version.</param>
165:     /// <param name="frontendPackageSet">Optional exact frontend package set, defaulting to the set belonging to the emitted era.</param>
166:     /// <returns>The validated profile.</returns>
167:     /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when a version, target framework or package version is not exact and supported.</exception>
168:     internal static CratisBackendApplicationScaffoldProfile Create(
169:         string version,
170:         string targetFramework,
171:         string cratisPackageVersion,
172:         string cratisArcMongoDBPackageVersion,
173:         string cratisArcChronicleTestingPackageVersion,
174:         string cratisSpecificationsPackageVersion,
175:         string cratisSpecificationsXUnitPackageVersion,
176:         string microsoftNetTestSdkPackageVersion,
177:         string nSubstitutePackageVersion,
178:         string xunitPackageVersion,
179:         string xunitRunnerVisualStudioPackageVersion,
180:         string chronicleImageVersion,
181:         CratisFrontendPackageSet? frontendPackageSet = null)
182:     {
183:         if (!IsPositiveInteger(version))
184:         {
185:             throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires a positive integer contract version.");
186:         }
187: 
188:         if (!IsTargetFramework(targetFramework))
189:         {
190:             throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires an exact net<major>.<minor> target framework.");
191:         }
192: 
193:         var exactVersions = new[]
194:         {
195:             cratisPackageVersion,
196:             cratisArcMongoDBPackageVersion,
197:             cratisArcChronicleTestingPackageVersion,
198:             cratisSpecificationsPackageVersion,
199:             cratisSpecificationsXUnitPackageVersion,
200:             microsoftNetTestSdkPackageVersion,
201:             nSubstitutePackageVersion,
202:             xunitPackageVersion,
203:             xunitRunnerVisualStudioPackageVersion,
204:             chronicleImageVersion
205:         };
206:         if (exactVersions.Any(versionValue => !IsExactStableVersion(versionValue)))
207:         {
208:             throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires exact stable major.minor.patch package and image versions.");
209:         }
210: 
211:         var frontend = frontendPackageSet ?? CratisFrontendPackageSet.Current;
212:         if (frontend.Versions().Any(versionValue => !IsExactStableVersion(versionValue)))
213:         {
214:             throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires exact stable major.minor.patch frontend package versions.");
215:         }
216: 
217:         return new(
218:             version,
219:             targetFramework,
220:             cratisPackageVersion,
221:             cratisArcMongoDBPackageVersion,
222:             cratisArcChronicleTestingPackageVersion,
223:             cratisSpecificationsPackageVersion,
224:             cratisSpecificationsXUnitPackageVersion,
225:             microsoftNetTestSdkPackageVersion,
226:             nSubstitutePackageVersion,
227:             xunitPackageVersion,
228:             xunitRunnerVisualStudioPackageVersion,
229:             chronicleImageVersion,
230:             frontend);
231:     }
232: 
233:     static bool IsTargetFramework(string value) =>
234:         value?.StartsWith("net", StringComparison.Ordinal) == true && IsExactVersion(value[3..], 2);
235: 
236:     static bool IsExactStableVersion(string value) => IsExactVersion(value, 3);
237: 
238:     static bool IsExactVersion(string value, int componentCount)
239:     {
240:         if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
241:         {
242:             return false;
243:         }
244: 
245:         var components = value.Split('.');
246:         return components.Length == componentCount && components.All(IsNonNegativeInteger);
247:     }
248: 
249:     static bool IsPositiveInteger(string value) =>
250:         IsNonNegativeInteger(value) && value.Any(character => character != '0');
251: 
252:     static bool IsNonNegativeInteger(string value) =>
253:         !string.IsNullOrEmpty(value) &&
254:         (value.Length == 1 || value[0] != '0') &&
255:         value.All(character => character is >= '0' and <= '9');
256: }
