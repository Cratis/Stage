1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: namespace Cratis.Stage.Rendering.Cratis.Scaffolding;
5: 
6: /// <summary>
7: /// Represents the exact npm package versions a scaffolded Cratis application composes its frontend from.
8: /// </summary>
9: /// <remarks>
10: /// <para>
11: /// This set is deliberately a sidecar to <see cref="CratisBackendApplicationScaffoldProfile"/> rather than
12: /// more members on it: the profile's public static surface is frozen to its verified <c language="csharp">Current</c>
13: /// profile, and the frontend era moves on npm publish cadence rather than on the NuGet cadence of the backend pins.
14: /// </para>
15: /// <para>
16: /// It is separate from the versions Stage itself builds with. Those are Stage's own <b>tooling</b> versions - the
17: /// packages the Stage repository compiles, tests and ships with - and they follow whatever Stage currently develops
18: /// against. The versions here are an <b>emitted era</b>: the versions a generated application is pinned to, chosen so
19: /// the generated application stays reproducible long after Stage's own tooling has moved forward.
20: /// </para>
21: /// <para>
22: /// It is equally separate from the emitted <b>backend</b> pins on <see cref="CratisBackendApplicationScaffoldProfile"/>.
23: /// Both describe the same emitted era, but they resolve from different registries and cannot be kept in lockstep by
24: /// sharing a single version string: only <c language="json">@cratis/arc</c> and its siblings share the Cratis major with the NuGet
25: /// <c language="csharp">Cratis</c> packages, while <c language="json">rxjs</c>, <c language="json">tsyringe</c> and <c language="json">reflect-metadata</c> have no
26: /// NuGet counterpart at all.
27: /// </para>
28: /// <para>
29: /// The Scene family is pinned at exact 4.10.0 for shared binding resolvers and typed runtime contracts, released default blueprint, package-owned PrimeReact mapping,
30: /// editable keyed lookups, explicit command inputs and identity-aware bindings, with Components 4.14.0 supplying the optional native-form footer. Frontend
31: /// installation and native consumer verification use those published packages as a separate gate;
32: /// C# rendering checks alone do not establish it.
33: /// </para>
34: /// </remarks>
35: public sealed class CratisFrontendPackageSet
36: {
37:     CratisFrontendPackageSet(
38:         string arcPackageVersion,
39:         string arcReactPackageVersion,
40:         string arcVitePackageVersion,
41:         string fundamentalsPackageVersion,
42:         string rxjsPackageVersion,
43:         string tsyringePackageVersion,
44:         string reflectMetadataPackageVersion,
45:         string reactPackageVersion,
46:         string reactDomPackageVersion,
47:         string reactRouterDomPackageVersion,
48:         string vitePackageVersion,
49:         string typeScriptPackageVersion,
50:         string vitePluginReactPackageVersion,
51:         string typesReactPackageVersion,
52:         string typesReactDomPackageVersion,
53:         string componentsPackageVersion,
54:         string scenePackageVersion,
55:         string primeReactPackageVersion,
56:         string primeIconsPackageVersion,
57:         string primeUixThemesPackageVersion,
58:         string typesNodePackageVersion)
59:     {
60:         ArcPackageVersion = arcPackageVersion;
61:         ArcReactPackageVersion = arcReactPackageVersion;
62:         ArcVitePackageVersion = arcVitePackageVersion;
63:         FundamentalsPackageVersion = fundamentalsPackageVersion;
64:         RxjsPackageVersion = rxjsPackageVersion;
65:         TsyringePackageVersion = tsyringePackageVersion;
66:         ReflectMetadataPackageVersion = reflectMetadataPackageVersion;
67:         ReactPackageVersion = reactPackageVersion;
68:         ReactDomPackageVersion = reactDomPackageVersion;
69:         ReactRouterDomPackageVersion = reactRouterDomPackageVersion;
70:         VitePackageVersion = vitePackageVersion;
71:         TypeScriptPackageVersion = typeScriptPackageVersion;
72:         VitePluginReactPackageVersion = vitePluginReactPackageVersion;
73:         TypesReactPackageVersion = typesReactPackageVersion;
74:         TypesReactDomPackageVersion = typesReactDomPackageVersion;
75:         ComponentsPackageVersion = componentsPackageVersion;
76:         ScenePackageVersion = scenePackageVersion;
77:         PrimeReactPackageVersion = primeReactPackageVersion;
78:         PrimeIconsPackageVersion = primeIconsPackageVersion;
79:         PrimeUixThemesPackageVersion = primeUixThemesPackageVersion;
80:         TypesNodePackageVersion = typesNodePackageVersion;
81:     }
82: 
83:     /// <summary>
84:     /// Gets the exact <c language="json">@cratis/arc</c> package version.
85:     /// </summary>
86:     public string ArcPackageVersion { get; }
87: 
88:     /// <summary>
89:     /// Gets the exact <c language="json">@cratis/arc.react</c> package version.
90:     /// </summary>
91:     public string ArcReactPackageVersion { get; }
92: 
93:     /// <summary>
94:     /// Gets the exact <c language="json">@cratis/arc.vite</c> package version.
95:     /// </summary>
96:     public string ArcVitePackageVersion { get; }
97: 
98:     /// <summary>
99:     /// Gets the exact <c language="json">@cratis/fundamentals</c> package version.
100:     /// </summary>
101:     public string FundamentalsPackageVersion { get; }
102: 
103:     /// <summary>
104:     /// Gets the exact <c language="json">rxjs</c> package version.
105:     /// </summary>
106:     public string RxjsPackageVersion { get; }
107: 
108:     /// <summary>
109:     /// Gets the exact <c language="json">tsyringe</c> package version.
110:     /// </summary>
111:     public string TsyringePackageVersion { get; }
112: 
113:     /// <summary>
114:     /// Gets the exact <c language="json">reflect-metadata</c> package version.
115:     /// </summary>
116:     public string ReflectMetadataPackageVersion { get; }
117: 
118:     /// <summary>
119:     /// Gets the exact <c language="json">react</c> package version.
120:     /// </summary>
121:     public string ReactPackageVersion { get; }
122: 
123:     /// <summary>
124:     /// Gets the exact <c language="json">react-dom</c> package version.
125:     /// </summary>
126:     public string ReactDomPackageVersion { get; }
127: 
128:     /// <summary>
129:     /// Gets the exact <c language="json">react-router-dom</c> package version.
130:     /// </summary>
131:     public string ReactRouterDomPackageVersion { get; }
132: 
133:     /// <summary>
134:     /// Gets the exact <c language="json">vite</c> package version.
135:     /// </summary>
136:     public string VitePackageVersion { get; }
137: 
138:     /// <summary>
139:     /// Gets the exact <c language="json">typescript</c> package version.
140:     /// </summary>
141:     public string TypeScriptPackageVersion { get; }
142: 
143:     /// <summary>
144:     /// Gets the exact <c language="json">@vitejs/plugin-react</c> package version.
145:     /// </summary>
146:     public string VitePluginReactPackageVersion { get; }
147: 
148:     /// <summary>
149:     /// Gets the exact <c language="json">@types/react</c> package version.
150:     /// </summary>
151:     public string TypesReactPackageVersion { get; }
152: 
153:     /// <summary>
154:     /// Gets the exact <c language="json">@types/react-dom</c> package version.
155:     /// </summary>
156:     public string TypesReactDomPackageVersion { get; }
157: 
158:     /// <summary>
159:     /// Gets the frontend package set belonging to the emitted era of the current scaffold profile.
160:     /// </summary>
161:     /// <summary>
162:     /// Gets the exact <c language="json">@cratis/components</c> package version.
163:     /// </summary>
164:     public string ComponentsPackageVersion { get; }
165: 
166:     /// <summary>
167:     /// Gets the exact <c language="json">@cratis/scene.*</c> package version, shared by every Scene package.
168:     /// </summary>
169:     public string ScenePackageVersion { get; }
170: 
171:     /// <summary>
172:     /// Gets the exact version shared by <c language="json">primereact</c> and every <c language="json">@primereact/*</c> package.
173:     /// </summary>
174:     /// <remarks>
175:     /// These are peer dependencies of <c language="json">@cratis/components</c>, which a composed screen renders through. A
176:     /// generated application that declared the component library without them would not install cleanly.
177:     /// </remarks>
178:     public string PrimeReactPackageVersion { get; }
179: 
180:     /// <summary>
181:     /// Gets the exact <c language="json">primeicons</c> package version.
182:     /// </summary>
183:     public string PrimeIconsPackageVersion { get; }
184: 
185:     /// <summary>
186:     /// Gets the exact <c language="json">@primeuix/themes</c> package version.
187:     /// </summary>
188:     public string PrimeUixThemesPackageVersion { get; }
189: 
190:     /// <summary>
191:     /// Gets the exact <c language="json">@types/node</c> package version.
192:     /// </summary>
193:     /// <remarks>
194:     /// The emitted bundler configuration resolves its own paths through <c language="json">node:url</c>, so the frontend does
195:     /// not type-check without these declarations. A generated application that cannot run its own build script
196:     /// is not finished, and the author cannot add the dependency to a file they are told not to edit.
197:     /// </remarks>
198:     public string TypesNodePackageVersion { get; }
199: 
200:     /// <summary>
201:     /// Gets the package set the current era emits.
202:     /// </summary>
203:     /// <remarks>
204:     /// Every package the emitted Stage frontend runtime imports is pinned to the version the Stage frontend itself
205:     /// builds with (<c language="shell">Source/Frontend/package.json</c>), so the runtime a generated application
206:     /// compiles is the runtime the live Stage was verified with. A specification compares the two.
207:     /// </remarks>
208:     internal static CratisFrontendPackageSet Current { get; } = new(
209:         "22.58.2",
210:         "22.58.2",
211:         "22.58.2",
212:         "7.22.8",
213:         "7.8.2",
214:         "4.10.0",
215:         "0.2.2",
216:         "19.3.0",
217:         "19.3.0",
218:         "7.18.4",
219:         "8.3.0",
220:         "7.0.2",
221:         "6.1.1",
222:         "19.3.0",
223:         "19.3.0",
224:         "4.26.2",
225:         "4.22.1",
226:         "11.2.0",
227:         "8.0.2",
228:         "3.0.1",
229:         "26.6.4");
230: 
231:     /// <summary>
232:     /// Creates an unvalidated frontend package set for in-assembly contract verification.
233:     /// </summary>
234:     /// <param name="arcPackageVersion">The exact <c language="json">@cratis/arc</c> package version.</param>
235:     /// <param name="arcReactPackageVersion">The exact <c language="json">@cratis/arc.react</c> package version.</param>
236:     /// <param name="arcVitePackageVersion">The exact <c language="json">@cratis/arc.vite</c> package version.</param>
237:     /// <param name="fundamentalsPackageVersion">The exact <c language="json">@cratis/fundamentals</c> package version.</param>
238:     /// <param name="rxjsPackageVersion">The exact <c language="json">rxjs</c> package version.</param>
239:     /// <param name="tsyringePackageVersion">The exact <c language="json">tsyringe</c> package version.</param>
240:     /// <param name="reflectMetadataPackageVersion">The exact <c language="json">reflect-metadata</c> package version.</param>
241:     /// <param name="reactPackageVersion">The exact <c language="json">react</c> package version.</param>
242:     /// <param name="reactDomPackageVersion">The exact <c language="json">react-dom</c> package version.</param>
243:     /// <param name="reactRouterDomPackageVersion">The exact <c language="json">react-router-dom</c> package version.</param>
244:     /// <param name="vitePackageVersion">The exact <c language="json">vite</c> package version.</param>
245:     /// <param name="typeScriptPackageVersion">The exact <c language="json">typescript</c> package version.</param>
246:     /// <param name="vitePluginReactPackageVersion">The exact <c language="json">@vitejs/plugin-react</c> package version.</param>
247:     /// <param name="typesReactPackageVersion">The exact <c language="json">@types/react</c> package version.</param>
248:     /// <param name="typesReactDomPackageVersion">The exact <c language="json">@types/react-dom</c> package version.</param>
249:     /// <param name="componentsPackageVersion">The exact <c language="json">@cratis/components</c> package version.</param>
250:     /// <param name="scenePackageVersion">The exact version shared by every <c language="json">@cratis/scene.*</c> package.</param>
251:     /// <param name="primeReactPackageVersion">The exact version shared by the PrimeReact packages.</param>
252:     /// <param name="primeIconsPackageVersion">The exact <c language="json">primeicons</c> package version.</param>
253:     /// <param name="primeUixThemesPackageVersion">The exact <c language="json">@primeuix/themes</c> package version.</param>
254:     /// <param name="typesNodePackageVersion">The exact <c language="json">@types/node</c> package version.</param>
255:     /// <returns>The frontend package set, validated only when a profile is created from it.</returns>
256:     internal static CratisFrontendPackageSet Create(
257:         string arcPackageVersion,
258:         string arcReactPackageVersion,
259:         string arcVitePackageVersion,
260:         string fundamentalsPackageVersion,
261:         string rxjsPackageVersion,
262:         string tsyringePackageVersion,
263:         string reflectMetadataPackageVersion,
264:         string reactPackageVersion,
265:         string reactDomPackageVersion,
266:         string reactRouterDomPackageVersion,
267:         string vitePackageVersion,
268:         string typeScriptPackageVersion,
269:         string vitePluginReactPackageVersion,
270:         string typesReactPackageVersion,
271:         string typesReactDomPackageVersion,
272:         string componentsPackageVersion,
273:         string scenePackageVersion,
274:         string primeReactPackageVersion,
275:         string primeIconsPackageVersion,
276:         string primeUixThemesPackageVersion,
277:         string typesNodePackageVersion) =>
278:         new(
279:             arcPackageVersion,
280:             arcReactPackageVersion,
281:             arcVitePackageVersion,
282:             fundamentalsPackageVersion,
283:             rxjsPackageVersion,
284:             tsyringePackageVersion,
285:             reflectMetadataPackageVersion,
286:             reactPackageVersion,
287:             reactDomPackageVersion,
288:             reactRouterDomPackageVersion,
289:             vitePackageVersion,
290:             typeScriptPackageVersion,
291:             vitePluginReactPackageVersion,
292:             typesReactPackageVersion,
293:             typesReactDomPackageVersion,
294:             componentsPackageVersion,
295:             scenePackageVersion,
296:             primeReactPackageVersion,
297:             primeIconsPackageVersion,
298:             primeUixThemesPackageVersion,
299:             typesNodePackageVersion);
300: 
301:     /// <summary>
302:     /// Gets every version in the set, for validation through the single exactness rule the scaffold profile owns.
303:     /// </summary>
304:     /// <returns>The exact versions in declaration order.</returns>
305:     internal IEnumerable<string> Versions()
306:     {
307:         yield return ArcPackageVersion;
308:         yield return ArcReactPackageVersion;
309:         yield return ArcVitePackageVersion;
310:         yield return FundamentalsPackageVersion;
311:         yield return RxjsPackageVersion;
312:         yield return TsyringePackageVersion;
313:         yield return ReflectMetadataPackageVersion;
314:         yield return ReactPackageVersion;
315:         yield return ReactDomPackageVersion;
316:         yield return ReactRouterDomPackageVersion;
317:         yield return VitePackageVersion;
318:         yield return TypeScriptPackageVersion;
319:         yield return VitePluginReactPackageVersion;
320:         yield return TypesReactPackageVersion;
321:         yield return TypesReactDomPackageVersion;
322:         yield return ComponentsPackageVersion;
323:         yield return ScenePackageVersion;
324:         yield return PrimeReactPackageVersion;
325:         yield return PrimeIconsPackageVersion;
326:         yield return PrimeUixThemesPackageVersion;
327:         yield return TypesNodePackageVersion;
328:     }
329: }
