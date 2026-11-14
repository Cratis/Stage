1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Contracts.Specifications.Semantic;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
8: 
9: namespace Cratis.Stage.Rendering.Cratis.Semantics;
10: 
11: /// <summary>
12: /// One ESM v5–v10 construct a Stage execution or rendering path refuses.
13: /// </summary>
14: /// <param name="Code">The typed STAGE-ESM diagnostic code.</param>
15: /// <param name="Capability">The execution capability the construct needs.</param>
16: /// <param name="Artifact">The declaration that uses the construct.</param>
17: /// <param name="Kind">The kind of declaration: trigger, slice, reaction, capture, command or specification.</param>
18: /// <param name="Message">What the construct is and why it is refused.</param>
19: internal sealed record SemanticVersionFeature(string Code, StageExecutionCapability Capability, SemanticId Artifact, string Kind, string Message);
20: 
21: /// <summary>
22: /// Finds the ESM v5–v10 constructs that renderer admission, the specification executor and the Host runtime refuse.
23: /// </summary>
24: /// <remarks>
25: /// One detection serves all three (the Host and the specification executor compile this file). Routes have separate
26: /// detectors so the Host can execute them while renderer and specification admission continue to refuse them.
27: /// </remarks>
28: internal static class SemanticVersionFeatures
29: {
30:     /// <summary>
31:     /// The code for ESM v6 triggers, reactions, captures, automation and translate slices and their specification forms.
32:     /// </summary>
33:     internal const string Automation = "STAGE-ESM-024";
34: 
35:     /// <summary>
36:     /// The code for ESM v5 keyed read-model absence expectations.
37:     /// </summary>
38:     internal const string Absence = "STAGE-ESM-027";
39: 
40:     /// <summary>
41:     /// The code for ESM v7 generated command values and generation fixtures.
42:     /// </summary>
43:     internal const string Generated = "STAGE-ESM-028";
44: 
45:     /// <summary>
46:     /// The code for ESM v7 command responses and return expectations.
47:     /// </summary>
48:     internal const string Responses = "STAGE-ESM-029";
49: 
50:     /// <summary>
51:     /// The code for generated-value or response combinations that cannot be preserved by the target.
52:     /// </summary>
53:     internal const string CommandResults = "STAGE-ESM-038";
54: 
55:     /// <summary>
56:     /// The code for ESM v8 event sources, streams and routes, shared with renderer admission and the semantic surface ledger.
57:     /// </summary>
58:     internal const string Routes = "STAGE-ESM-030";
59: 
60:     /// <summary>
61:     /// The code for ESM v9 public-event publication.
62:     /// </summary>
63:     internal const string PublicEvents = "STAGE-ESM-032";
64: 
65:     /// <summary>
66:     /// The code for ESM v9 foreign events and events-source captures.
67:     /// </summary>
68:     internal const string ForeignEvents = "STAGE-ESM-033";
69: 
70:     /// <summary>
71:     /// The code for ESM v10 reaction execution identity.
72:     /// </summary>
73:     internal const string ReactionIdentity = "STAGE-ESM-034";
74: 
75:     /// <summary>
76:     /// The code for ESM v10 reaction and reducer observer filters.
77:     /// </summary>
78:     internal const string ObserverFilters = "STAGE-ESM-035";
79: 
80:     /// <summary>
81:     /// The code for ESM v10 per-production route overrides.
82:     /// </summary>
83:     internal const string ProductionRoutes = "STAGE-ESM-036";
84: 
85:     /// <summary>
86:     /// The code for a reaction trigger that both produces events and invokes commands.
87:     /// </summary>
88:     internal const string MixedReactionEffects = "STAGE-ESM-037";
89: 
90:     /// <summary>
91:     /// The code for compliance on identity uses or enumeration concepts.
92:     /// </summary>
93:     internal const string ProtectedIdentity = "STAGE-ESM-040";
94: 
95:     /// <summary>
96:     /// Checks the language and semantic version pair Stage has audited.
97:     /// </summary>
98:     /// <param name="model">The executable model.</param>
99:     /// <returns>Whether Stage supports the version pair.</returns>
100:     public static bool Supports(ExecutableSemanticModel model) => EsmSchemaV13Support.Supports(model.LanguageVersion, model.SemanticVersion);
101: 
102:     /// <summary>
103:     /// Gets every slice in the application in model order, including slices of nested features.
104:     /// </summary>
105:     /// <param name="application">The semantic application.</param>
106:     /// <returns>The slices.</returns>
107:     public static IEnumerable<SemanticSlice> Slices(SemanticApplication application) =>
108:         application.Modules.SelectMany(module => module.Features).SelectMany(Slices);
109: 
110:     /// <summary>
111:     /// Finds application-level constructs that belong to no slice.
112:     /// </summary>
113:     /// <param name="application">The semantic application.</param>
114:     /// <returns>The refused constructs.</returns>
115:     public static IEnumerable<SemanticVersionFeature> InApplication(SemanticApplication application) =>
116:         (application.Triggers.IsDefault ? [] : application.Triggers).Select(trigger => new SemanticVersionFeature(
117:             Automation, StageExecutionCapability.Reaction, trigger.Id, "trigger", $"Application trigger '{trigger.Name}' is an ESM v6 automation construct, which Stage does not support yet."))
118:         .Concat(RoutesInApplication(application))
119:         .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(command => UnsupportedGenerated(command, application.Concepts)));
120: 
121:     /// <summary>
122:     /// Finds named event sources and streams the renderer cannot render yet.
123:     /// </summary>
124:     /// <param name="application">The semantic application.</param>
125:     /// <returns>The routed declarations.</returns>
126:     public static IEnumerable<SemanticVersionFeature> RoutesInApplication(SemanticApplication application) =>
127:         (application.EventSources.IsDefault ? [] : application.EventSources).Select(source => new SemanticVersionFeature(
128:             Routes, StageExecutionCapability.IdentityAllocation, source.Id, "eventsource", "Named event sources and streams are not yet supported by the Cratis ESM planner."));
129: 
130:     /// <summary>
131:     /// Finds the structural automation and public-event constructs of one slice.
132:     /// </summary>
133:     /// <param name="slice">The slice.</param>
134:     /// <returns>The refused constructs.</returns>
135:     public static IEnumerable<SemanticVersionFeature> InSlice(SemanticSlice slice)
136:     {
137:         // Precise reaction constructs precede the generic slice refusal for first-refusal consumers.
138:         foreach (var feature in ReactionsInSlice(slice))
139:         {
140:             yield return feature;
141:         }
142: 
143:         foreach (var filter in ReducerFiltersInSlice(slice)) yield return filter;
144: 
145:         foreach (var feature in AutomationSlices(slice)) yield return feature;
146: 
147:         if (slice.Kind == SemanticSliceKind.Translate)
148:         {
149:             var direction = $" ({slice.Direction?.ToString() ?? "Inbound"})";
150:             yield return new(Automation, StageExecutionCapability.Reaction, slice.Id, "slice", $"Slice '{slice.Name}' is an ESM v6 {slice.Kind}{direction} slice; Stage renders only StateChange and StateView slices.");
151:         }
152: 
153:         foreach (var @event in slice.Events)
154:         {
155:             if (InEvent(@event) is { } feature) yield return feature;
156:         }
157: 
158:         foreach (var projection in slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event))
159:         {
160:             yield return new(PublicEvents, StageExecutionCapability.Projection, projection.Id, "projection", $"Projection '{projection.Name}' publishes an ESM v9 public event, which Stage does not support yet.");
161:         }
162: 
163:         foreach (var reducer in slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event))
164:         {
165:             yield return new(PublicEvents, StageExecutionCapability.Projection, reducer.ReadModel, "reducer", $"Reducer '{reducer.Name}' publishes an ESM v9 public event, which Stage does not support yet.");
166:         }
167: 
168:         foreach (var capture in slice.Captures.IsDefault ? [] : slice.Captures)
169:         {
170:             if (capture.EventsSource is not null)
171:             {
172:                 yield return new(ForeignEvents, StageExecutionCapability.ExternalEffect, capture.Id, "capture", $"Capture '{capture.Name}' consumes ESM v9 source events, which Stage does not support yet.");
173:             }
174:             else
175:             {
176:                 yield return new(Automation, StageExecutionCapability.ExternalEffect, capture.Id, "capture", $"Capture '{capture.Name}' is an ESM v6 translation construct, which Stage does not support yet.");
177:             }
178:         }
179:     }
180: 
181:     /// <summary>
182:     /// Finds every refused reaction construct, with precise reasons before the generic reaction refusal.
183:     /// </summary>
184:     /// <param name="slice">The slice.</param>
185:     /// <returns>The refused reaction constructs, including event reactions not yet admitted for rendering.</returns>
186:     public static IEnumerable<SemanticVersionFeature> ReactionsInSlice(SemanticSlice slice)
187:     {
188:         var reactions = slice.Reactions.IsDefault ? [] : slice.Reactions;
189:         foreach (var reaction in reactions.Where(reaction => reaction.RunsAs is not null))
190:         {
191:             yield return new(ReactionIdentity, StageExecutionCapability.Authorization, reaction.Id, "reaction", $"Reaction '{reaction.Name}' declares an ESM v10 execution identity, which Stage does not support yet.");
192:         }
193: 
194:         foreach (var reaction in reactions.Where(reaction => reaction.From is not null))
195:         {
196:             yield return new(ObserverFilters, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' declares an ESM v10 observer filter, which Stage does not support yet.");
197:         }
198: 
199:         foreach (var feature in reactions.SelectMany(MixedEffectsInReaction)) yield return feature;
200: 
201:         foreach (var feature in reactions.SelectMany(UnsupportedReactionContexts)) yield return feature;
202: 
203:         foreach (var reaction in reactions)
204:         {
205:             foreach (var trigger in reaction.Triggers)
206:             {
207:                 if (trigger.RequirementId is not null)
208:                 {
209:                     yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' has an opaque body for trigger kind '{trigger.Kind}', which Stage does not support yet.");
210:                 }
211: 
212:                 if (trigger.Kind != SemanticReactionTriggerKind.Event)
213:                 {
214:                     yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' uses trigger kind '{trigger.Kind}', which Stage does not support yet.");
215:                 }
216:             }
217:         }
218: 
219:         foreach (var feature in EventReactionsInSlice(slice)) yield return feature;
220:     }
221: 
222:     /// <summary>
223:     /// Finds Automation slices the specification executor still refuses.
224:     /// </summary>
225:     /// <param name="slice">The slice.</param>
226:     /// <returns>The execution-only slice refusal.</returns>
227:     public static IEnumerable<SemanticVersionFeature> AutomationSlices(SemanticSlice slice)
228:     {
229:         if (slice.Kind == SemanticSliceKind.Automation)
230:         {
231:             yield return new(Automation, StageExecutionCapability.Reaction, slice.Id, "slice", $"Slice '{slice.Name}' is an ESM v6 Automation slice; targets must explicitly admit its reaction behavior.");
232:         }
233:     }
234: 
235:     /// <summary>
236:     /// Finds event reactions that targets must explicitly admit.
237:     /// </summary>
238:     /// <param name="slice">The slice.</param>
239:     /// <returns>The execution-only reaction refusals.</returns>
240:     public static IEnumerable<SemanticVersionFeature> EventReactionsInSlice(SemanticSlice slice) =>
241:         (slice.Reactions.IsDefault ? [] : slice.Reactions).Select(reaction => new SemanticVersionFeature(
242:             Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' is an ESM v6 automation construct; targets must explicitly admit its event-triggered behavior."));
243: 
244:     /// <summary>
245:     /// Finds public or foreign events declared elsewhere but referenced by a slice.
246:     /// </summary>
247:     /// <param name="slice">The referencing slice.</param>
248:     /// <param name="events">The application's indexed event contracts.</param>
249:     /// <returns>The refused event contracts.</returns>
250:     public static IEnumerable<SemanticVersionFeature> EventReferences(SemanticSlice slice, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
251:     {
252:         var references = slice.Projections.SelectMany(ProjectionReferencedEventNamesAreUnique.Contracts)
253:             .Concat(slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event).Select(projection => projection.ReadModel))
254:             .Concat(slice.Reducers.SelectMany(reducer => reducer.Transitions.Select(transition => transition.EventContract)))
255:             .Concat(slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event).Select(reducer => reducer.ReadModel))
256:             .Concat(slice.Commands.SelectMany(command => command.Produces.Select(produced => produced.EventContract)))
257:             .Concat(ConstraintEvents(slice.Constraints))
258:             .Concat((slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers)
259:                 .SelectMany(trigger => trigger.Produces.Select(produced => produced.EventContract)
260:                     .Concat(trigger.Kind == SemanticReactionTriggerKind.Event ? [trigger.Source] : [])))
261:             .Concat((slice.Captures.IsDefault ? [] : slice.Captures).SelectMany(capture =>
262:                 (capture.EventsSource?.Events ?? []).Concat(capture.Appends.Select(append => append.EventContract))
263:                     .Concat(capture.Children.SelectMany(children => children.Appends.Select(append => append.EventContract)))
264:                     .Concat(capture.Nested.SelectMany(nested => nested.Appends.Select(append => append.EventContract)))))
265:             .Concat(slice.Specifications.SelectMany(specification => specification.GivenEvents.Concat(specification.ThenEvents)
266:                 .Select(occurrence => occurrence.EventContract)
267:                 .Concat(specification.WhenAppended is { } appended ? [appended.EventContract] : [])))
268:             .Except(slice.Events.Select(@event => @event.Id));
269: 
270:         return EventReferences(references, events);
271:     }
272: 
273:     /// <summary>
274:     /// Finds public or foreign events named by the constraints a consumer selects.
275:     /// </summary>
276:     /// <param name="constraints">The selected constraints.</param>
277:     /// <param name="events">The application's indexed event contracts.</param>
278:     /// <returns>The refused event contracts.</returns>
279:     public static IEnumerable<SemanticVersionFeature> EventReferences(IEnumerable<SemanticConstraint> constraints, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
280:         EventReferences(ConstraintEvents(constraints), events);
281: 
282:     /// <summary>
283:     /// Finds the constructs one command uses: generated properties and a response.
284:     /// </summary>
285:     /// <param name="command">The command.</param>
286:     /// <returns>The refused constructs.</returns>
287:     public static IEnumerable<SemanticVersionFeature> InCommand(SemanticCommand command)
288:     {
289:         foreach (var route in RoutesInCommand(command))
290:         {
291:             yield return route;
292:         }
293: 
294:         foreach (var route in ProductionRoutesInCommand(command))
295:         {
296:             yield return route;
297:         }
298: 
299:         foreach (var feature in UnsupportedGenerated(command, null)) yield return feature;
300: 
301:         if (UnsupportedCommandResults(command))
302:         {
303:             yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' combines generated values or a response with routing, legacy destination allocation, generated-property rules, or a non-scalar/non-command response source; Stage cannot preserve that combination yet.");
304:         }
305:     }
306: 
307:     /// <summary>
308:     /// Finds the constructs one specification uses.
309:     /// </summary>
310:     /// <param name="specification">The specification.</param>
311:     /// <returns>The refused constructs.</returns>
312:     public static IEnumerable<SemanticVersionFeature> InSpecification(SemanticSpecification specification)
313:     {
314:         foreach (var route in RoutesInSpecification(specification))
315:         {
316:             yield return route;
317:         }
318: 
319:         if (specification.GivenClock is not null || specification.WhenClock is not null || specification.WhenTrigger is not null ||
320:             specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty)
321:         {
322:             var capability = specification.GivenClock is not null || specification.WhenClock is not null ? StageExecutionCapability.Time : StageExecutionCapability.Reaction;
323:             yield return new(Automation, capability, specification.Id, "specification", $"Specification '{specification.Name}' uses an ESM v6 clock, trigger or capture, which Stage does not support yet.");
324:         }
325: 
326:         if (!specification.ThenAbsentReadModels.IsDefaultOrEmpty)
327:         {
328:             yield return new(Absence, StageExecutionCapability.Projection, specification.Id, "specification", $"Specification '{specification.Name}' asserts a read model is absent (ESM v5); the generated ReadModelScenario exposes only a materialized record, so Stage does not support absence yet.");
329:         }
330:     }
331: 
332:     /// <summary>
333:     /// Finds a command route the renderer cannot render yet.
334:     /// </summary>
335:     /// <param name="command">The command.</param>
336:     /// <returns>The routed command, if any.</returns>
337:     public static IEnumerable<SemanticVersionFeature> RoutesInCommand(SemanticCommand command)
338:     {
339:         if (command.Route is not null)
340:         {
341:             yield return new(Routes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' has an event-source route, which the Cratis ESM planner cannot render yet.");
342:         }
343:     }
344: 
345:     /// <summary>
346:     /// Finds per-production route overrides that no Stage execution path supports yet.
347:     /// </summary>
348:     /// <param name="command">The command.</param>
349:     /// <returns>The refused command, if any.</returns>
350:     public static IEnumerable<SemanticVersionFeature> ProductionRoutesInCommand(SemanticCommand command)
351:     {
352:         if (command.Produces.Any(produced => produced.Route is not null))
353:         {
354:             yield return new(ProductionRoutes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' declares an ESM v10 production route override, which Stage does not support yet.");
355:         }
356:     }
357: 
358:     /// <summary>
359:     /// Finds routing fixtures and assertions the specification executor cannot execute yet.
360:     /// </summary>
361:     /// <param name="specification">The specification.</param>
362:     /// <returns>The routed specification, if any.</returns>
363:     public static IEnumerable<SemanticVersionFeature> RoutesInSpecification(SemanticSpecification specification)
364:     {
365:         if (specification.WhenAppended?.Route is not null || specification.GivenEvents.Concat(specification.ThenEvents).Any(occurrence => occurrence.Route is not null || occurrence.Unrouted))
366:         {
367:             yield return new(Routes, StageExecutionCapability.IdentityAllocation, specification.Id, "specification", $"Specification '{specification.Name}' uses event-source routing assertions, which the Cratis ESM planner cannot render yet.");
368:         }
369:     }
370: 
371:     /// <summary>
372:     /// Finds reducer filters that rendering admits but the Host and specification executor still refuse.
373:     /// </summary>
374:     /// <param name="slice">The slice.</param>
375:     /// <returns>The filtered reducers.</returns>
376:     public static IEnumerable<SemanticVersionFeature> ReducerFiltersInSlice(SemanticSlice slice) =>
377:         slice.Reducers.Where(reducer => reducer.From is not null).Select(reducer => new SemanticVersionFeature(
378:             ObserverFilters, StageExecutionCapability.Projection, reducer.ReadModel, "reducer", $"Reducer '{reducer.Name}' declares an ESM v10 observer filter, which Stage does not support yet."));
379: 
380:     internal static IEnumerable<SemanticVersionFeature> UnsupportedGenerated(SemanticCommand command, IEnumerable<SemanticConcept>? concepts)
381:     {
382:         foreach (var property in command.Properties.Where(property => property.IsGenerated).Where(property => UnsupportedGeneratedType(property.Type, concepts)))
383:         {
384:             yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}'; phase 1 renders non-optional, non-collection UUID-backed concepts only.");
385:         }
386:         if (concepts is not null)
387:         {
388:             foreach (var concept in concepts.Where(concept => command.Properties.Any(property => property.IsGenerated && property.Type.Target == concept.Id)))
389:             {
390:                 if (GeneratedPascalCase.From(concept.Name) == "New")
391:                 {
392:                     yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Generated concept '{concept.Name}' collides with its New factory.");
393:                 }
394:                 if (!concept.Validations.IsEmpty)
395:                 {
396:                     yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Generated concept '{concept.Name}' has validation rules; Arc reports a generation defect as an exception, not the evaluator's rejection.");
397:                 }
398:             }
399:         }
400:     }
401: 
402:     static IEnumerable<SemanticVersionFeature> MixedEffectsInReaction(SemanticReaction reaction)
403:     {
404:         foreach (var trigger in reaction.Triggers.Where(trigger => !trigger.Produces.IsDefaultOrEmpty && !trigger.Invokes.IsDefaultOrEmpty))
405:         {
406:             yield return new(MixedReactionEffects, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' both produces events and invokes commands in one '{trigger.Kind}' trigger; a failed command would make Chronicle redeliver the trigger and append the reaction's events again until reactor appends are idempotent (Cratis/Stage#342).");
407:         }
408:         foreach (var group in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event).GroupBy(trigger => trigger.Source).Where(HasMixedTriggerGroup))
409:         {
410:             yield return new(MixedReactionEffects, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' produces events and invokes commands across triggers on event '{group.Key}'; a failed command would make Chronicle redeliver the trigger and append the reaction's events again until reactor appends are idempotent (Cratis/Stage#342).");
411:         }
412:     }
413: 
414:     static bool HasMixedTriggerGroup(IGrouping<SemanticId, SemanticReactionTrigger> group) =>
415:         group.Count() > 1 && group.Any(trigger => !trigger.Produces.IsEmpty) && group.Any(trigger => !trigger.Invokes.IsEmpty);
416: 
417:     static IEnumerable<SemanticVersionFeature> UnsupportedReactionContexts(SemanticReaction reaction)
418:     {
419:         var contexts = ReactionContextExpressions(reaction).ToArray();
420:         if (contexts.Any(context => context.Value == SemanticEventContextValueKind.EventSourceIdentity))
421:         {
422:             yield return new("STAGE-ESM-013", StageExecutionCapability.IdentityAllocation, reaction.Id, "reaction", $"Reaction '{reaction.Name}' maps $context.eventSourceId, which Stage does not support yet.");
423:         }
424:         foreach (var context in contexts.Where(context => IsCausedBy(context.Value)).Select(context => context.Value).Distinct())
425:         {
426:             var member = CausedByMember(context);
427:             yield return new("STAGE-ESM-013", StageExecutionCapability.Occurrence, reaction.Id, "reaction", $"Reaction '{reaction.Name}' maps $context.causedBy.{member}, which Stage does not support yet.");
428:         }
429:     }
430: 
431:     static IEnumerable<SemanticEventContextExpression> ReactionContextExpressions(SemanticReaction reaction) =>
432:         reaction.Triggers.SelectMany(trigger => trigger.Produces.SelectMany(produced =>
433:                 produced.Mappings.Select(mapping => mapping.Source).Concat(produced.Destination is { } destination ? [destination] : []))
434:             .Concat(trigger.Invokes.SelectMany(invocation => invocation.Mappings.Select(mapping => mapping.Source))))
435:             .OfType<SemanticEventContextExpression>();
436: 
437:     static bool IsCausedBy(SemanticEventContextValueKind value) => value is
438:         SemanticEventContextValueKind.CausedBySubject or SemanticEventContextValueKind.CausedByName or SemanticEventContextValueKind.CausedByUserName;
439: 
440:     static string CausedByMember(SemanticEventContextValueKind value) => value switch
441:     {
442:         SemanticEventContextValueKind.CausedBySubject => "subject",
443:         SemanticEventContextValueKind.CausedByName => "name",
444:         _ => "userName"
445:     };
446: 
447:     static bool UnsupportedGeneratedType(SemanticTypeReference type, IEnumerable<SemanticConcept>? concepts)
448:     {
449:         if (type is not { Kind: SemanticTypeReferenceKind.Concept, IsOptional: false, IsCollection: false }) return true;
450: 
451:         return concepts?.Any(concept => concept.Id == type.Target && concept.Primitive == SemanticPrimitiveType.Uuid) == false;
452:     }
453: 
454:     static bool UnsupportedCommandResults(SemanticCommand command) =>
455:         UnsupportedGeneratedCombination(command) || UnsupportedResponse(command);
456: 
457:     static bool UnsupportedGeneratedCombination(SemanticCommand command)
458:     {
459:         var generated = command.Properties.Where(property => property.IsGenerated).ToArray();
460:         if (HasRoutedResults(command, generated.Length)) return true;
461:         if (generated.Length == 0) return false;
462:         if (generated.Any(property => property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null)) return true;
463:         if (OccurrenceParameterCollides(command, generated)) return true;
464: 
465:         return !command.Requirements.IsEmpty || command.Validations.Any(rule => generated.Any(property => property.Id == rule.Property));
466:     }
467: 
468:     static bool HasRoutedResults(SemanticCommand command, int generatedCount) =>
469:         command.Route is not null && (command.Response is not null || generatedCount > 0);
470: 
471:     static bool OccurrenceParameterCollides(SemanticCommand command, IEnumerable<SemanticProperty> generated) =>
472:         command.Produces.Any(produced => produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred })) &&
473:         generated.Any(property => IsOccurrenceParameter(property.Name));
474: 
475:     static bool IsOccurrenceParameter(string name)
476:     {
477:         var generatedName = GeneratedPascalCase.From(name);
478: 
479:         return string.Equals(generatedName, "Operation", StringComparison.Ordinal) || string.Equals(generatedName, "Occurred", StringComparison.Ordinal);
480:     }
481: 
482:     static bool UnsupportedResponse(SemanticCommand command) => command.Response switch
483:     {
484:         SemanticScalarCommandResponse scalar => UnsupportedResponseSource(command, scalar.Source, scalar.Type),
485:         SemanticRecordCommandResponse record =>
486:             record.Fields.Any(field => UnsupportedResponseSource(command, field.Source, field.Type)) ||
487:             !GeneratedPascalCase.RecordMembersAreUnique(command.Name + "Response", record.Fields.Select(field => field.Name)),
488:         _ => false
489:     };
490: 
491:     static bool UnsupportedResponseSource(SemanticCommand command, SemanticId source, SemanticTypeReference type) =>
492:         type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType ||
493:         !command.Properties.Any(property => property.Id == source && property.Type == type);
494: 
495:     static IEnumerable<SemanticId> ConstraintEvents(IEnumerable<SemanticConstraint> constraints) =>
496:         constraints.SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy));
497: 
498:     static IEnumerable<SemanticVersionFeature> EventReferences(IEnumerable<SemanticId> references, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
499:     {
500:         foreach (var id in references.Distinct())
501:         {
502:             if (events.TryGetValue(id, out var @event) && InEvent(@event) is { } feature) yield return feature;
503:         }
504:     }
505: 
506:     static SemanticVersionFeature? InEvent(SemanticEventContract @event)
507:     {
508:         if (@event.Origin is not null)
509:         {
510:             return new(ForeignEvents, StageExecutionCapability.ExternalEffect, @event.Id, "event", $"Event '{@event.Name}' originates from '{@event.Origin}' (ESM v9), which Stage does not support yet.");
511:         }
512: 
513:         return @event.Visibility == SemanticEventVisibility.Public
514:             ? new(PublicEvents, StageExecutionCapability.ExternalEffect, @event.Id, "event", $"Event '{@event.Name}' is an ESM v9 public contract, which Stage cannot publish yet.")
515:             : null;
516:     }
517: 
518:     static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
519: }
