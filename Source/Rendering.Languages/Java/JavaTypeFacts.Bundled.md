# Bundled Java type facts

The bundle is the union of JDK 21 and JDK 25 deep inheritable names, checked against Oracle's API documentation on October 28, 2026. These are binding hazards, not a list of all public API members. Unknown types remain unknown; they are not assigned empty facts.

## Sources and method

The complete top-level `java.lang` inventory is the union of these two package summaries:

- <https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/package-summary.html>
- <https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/package-summary.html>

For **each** name in `JavaLangTypes.Names`, the class-page URLs checked are the following bases plus `java/lang/<Name>.html` (nested classes use dots, not `$`):

- `https://docs.oracle.com/en/java/javase/21/docs/api/java.base/`
- `https://docs.oracle.com/en/java/javase/25/docs/api/java.base/`

`IO` and `StableValue` exist only in the JDK 25 inventory, and `StringTemplate` only in JDK 21. Missing pages do not establish empty facts: the other release supplies the entry. Each existing page's Nested Class Summary, Field Summary and inherited-member rows were inspected. Accessible nested types were inspected recursively, including inherited nested types. The common types outside `java.lang` are listed below with their paths under both bases.

Deep names of T are its accessible member-type names and non-private fields **if user code can extend or implement T**, plus deep names of all accessible member types. A final enclosing class can therefore have nonempty deep names. Ordinary methods, constructors, private fields and fields of final classes do not contribute. The bundle conservatively retains the names of sealed interfaces as well, rather than relying on their permits lists.

## Nonempty facts

The following table lists the complete nonempty entries. Each path identifies the class-page URL under **both** bases, except the JDK 21-only StringTemplate family. All unlisted top-level `java.lang` entries are known-empty after this traversal.

| Class-page path | Deep names (JDK 21 ∪ 25) |
| --- | --- |
| `java/lang/Enum.html` | EnumDesc |
| `java/lang/Thread.html` | Builder, OfPlatform, OfVirtual, State, UncaughtExceptionHandler, MAX_PRIORITY, MIN_PRIORITY, NORM_PRIORITY |
| `java/lang/Thread.Builder.html` | OfPlatform, OfVirtual |
| `java/lang/Thread.Builder.OfPlatform.html` | OfPlatform, OfVirtual (inherited from Builder; conservative for sealed interfaces) |
| `java/lang/Thread.Builder.OfVirtual.html` | OfPlatform, OfVirtual (inherited from Builder; conservative for sealed interfaces) |
| `java/lang/ProcessHandle.html` | Info |
| `java/lang/StringTemplate.html` | Processor, Linkage, RAW, STR |
| `java/lang/StringTemplate.Processor.html` | Linkage |
| `java/lang/ProcessBuilder.html` | Type, PIPE, INHERIT, DISCARD (through its extensible Redirect member) |
| `java/lang/ProcessBuilder.Redirect.html` | Type, PIPE, INHERIT, DISCARD |
| `java/lang/System.html` | Level (through its Logger member) |
| `java/lang/System.Logger.html` | Level |
| `java/lang/Class.html` | OfField, OfMethod (through inherited TypeDescriptor member types) |
| `java/lang/invoke/TypeDescriptor.html` | OfField, OfMethod |
| `java/lang/invoke/TypeDescriptor.OfField.html` | OfField, OfMethod (inherited from TypeDescriptor) |
| `java/lang/invoke/TypeDescriptor.OfMethod.html` | OfField, OfMethod (inherited from TypeDescriptor) |
| `java/math/BigDecimal.html` | ZERO, ONE, TWO, TEN, ROUND_UP, ROUND_DOWN, ROUND_CEILING, ROUND_FLOOR, ROUND_HALF_UP, ROUND_HALF_DOWN, ROUND_HALF_EVEN, ROUND_UNNECESSARY |

Useful direct source links for the recursive and inherited cases:

- [JDK 21 Thread](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/Thread.html) and [JDK 25 Thread](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/Thread.html)
- [JDK 21 Thread.Builder](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/Thread.Builder.html) and [JDK 25 Thread.Builder](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/Thread.Builder.html)
- [JDK 21 StringTemplate.Processor](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/StringTemplate.Processor.html)
- [JDK 21 ProcessBuilder.Redirect](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/ProcessBuilder.Redirect.html) and [JDK 25 ProcessBuilder.Redirect](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/ProcessBuilder.Redirect.html)
- [JDK 21 System.Logger](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/System.Logger.html) and [JDK 25 System.Logger](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/System.Logger.html)
- [JDK 21 Class](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/Class.html) and [JDK 25 Class](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/Class.html)
- [JDK 21 TypeDescriptor.OfField](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/invoke/TypeDescriptor.OfField.html) and [JDK 25 TypeDescriptor.OfField](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/invoke/TypeDescriptor.OfField.html)
- [JDK 21 BigDecimal](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/math/BigDecimal.html) and [JDK 25 BigDecimal](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/math/BigDecimal.html)

## Known-empty nested and common types

These additional canonical types are bundled as known-empty. The following paths identify the reviewed pages under both bases unless a release is explicitly stated:

- `java/lang/Character.Subset.html`, `java/lang/Character.UnicodeBlock.html`, `java/lang/Character.UnicodeScript.html`
- `java/lang/Enum.EnumDesc.html`, `java/lang/ModuleLayer.Controller.html`
- `java/lang/ProcessBuilder.Redirect.Type.html`, `java/lang/ProcessHandle.Info.html`
- `java/lang/Runtime.Version.html`, `java/lang/ScopedValue.Carrier.html`
- `java/lang/ScopedValue.CallableOp.html` (JDK 25 only)
- `java/lang/StackWalker.Option.html`, `java/lang/StackWalker.StackFrame.html`
- `java/lang/StringTemplate.Processor.Linkage.html` (JDK 21 only)
- `java/lang/System.Logger.Level.html`, `java/lang/System.LoggerFinder.html`
- `java/lang/Thread.State.html`, `java/lang/Thread.UncaughtExceptionHandler.html`
- `java/lang/annotation/Annotation.html`, `java/util/UUID.html`, `java/util/List.html`
- `java/time/LocalDate.html`, `java/time/OffsetDateTime.html`

`Character` is final. Its extensible `Subset` member has no inheritable fields or member types; `UnicodeBlock` and `UnicodeScript` are final. Their many constants therefore do not contribute. The inherited `EnumDesc` member of an enum has no deep names either. This leaves Character's deep set empty. See [JDK 21 Character](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/Character.html) and [JDK 25 Character](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/Character.html).

Similarly, the constants of final `LocalDate` and `OffsetDateTime` do not enter their sets. `Runtime` has no accessible constructor in JDK 21 and is final in JDK 25; its final Version member contributes nothing. `System.LoggerFinder` is extensible but has no member types or fields. `StackWalker`, `ModuleLayer` and `ScopedValue` have no deep names through their accessible members. Sealed OfPlatform and OfVirtual receive a conservative nonempty set, but their recursively inherited names reach a fixed point.
