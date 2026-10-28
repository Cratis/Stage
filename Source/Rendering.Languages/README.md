# Stage language packs

Prerelease, non-packable language packs for the declarative renderer. The first pack is `java@1`.

## Java declaration registry

`JavaPlan.Create(files)` admits all mapped `DeclarativeLanguageFile` values together. Its immutable `Declarations` map is keyed by the original `TemplateSymbol`, with each entry containing the allocated symbol and declaring file. Repeated handles in one file (for example constructors) share that entry. `Files` retains the input order and original segments; emission is a separate step.

A package and every dotted name segment must match `[A-Za-z_][A-Za-z0-9_]*`, excluding `_` and Java reserved or contextual words. Dollar signs are not structured nested-type separators: use `Outer.Inner`. Default-package symbols refuse with `STAGE-JAVA-0013`; invalid segments refuse with `STAGE-JAVA-0001`.

A path ending in ordinal `.java` must contain one imports segment before its first declaration. That declaration must be the top-level type matching the file basename; there must be exactly one distinct top-level symbol, and each nested declaration's immediate prefix must be declared in the same file and package. Decoded literal text before the first declaration cannot supply `package` or `import` words, even in comments. Violations refuse with `STAGE-JAVA-0007`. Other files may contain references, but declaration or imports handles refuse with `STAGE-JAVA-0005`.

Across the plan, duplicate declarations in different files refuse with `STAGE-JAVA-0008`. Packages (including their prefixes) and same-package top-level names that differ only in ordinal case refuse with `STAGE-JAVA-0009`. A top-level type whose canonical name is also a plan package refuses with `STAGE-JAVA-0014`. Top-level names are never renamed.

Nested declarations are compared only with immediate siblings and enclosing names. A name equal to an enclosing name, or siblings differing only in ordinal case, is allocated as `Name_<hash>`. The hash starts with eight lowercase hex digits of SHA-256 over `package + '\n' + original dotted name`. All colliding candidates are extended by four digits at a time, without a visit-order winner, avoiding sibling and enclosing names and every literal or authored word in the file. Beyond the first digest, further digits come from SHA-256 over the same identity plus `'\n' + digitsAlreadyAvailable`. An allocation exceeding 255 ASCII bytes refuses with `STAGE-JAVA-0001`. Descendants always use their parent's allocated prefix.

## Binding guarantee

The pack parses no Java. Every spelling it writes for a structured `Reference` is either provably bound or refused.

- **Short** when the simple name is free: it is not a word the file's text, an inherited member (from the facts) or a referenced plan file could bind, and it is not a package's first segment. Same-package plan types are written short; foreign types are imported once, sorted.
- **Imported explicitly** for every `java.lang` type the plan or a primitive spells, so a same-package type the plan does not know about cannot capture it.
- **Qualified** when the short name is not free. A qualified name is refused (`STAGE-JAVA-0010`) when its first segment could be obscured.
- A spelling is also refused (`STAGE-JAVA-0006`) when the text before it ends in `.` or `/`, when its line holds `//` or `/*` before it, or when it touches an identifier character.
- Closure over referenced plan files is conservative: the pack cannot tell whether a file extends a referenced type, so a referenced file's words flow into the referencing file. A known consequence is that a plan record whose own annotation string repeats its name (`@Command(name = "MoveOrder")`) is referenced qualified; the 166 such K1 spellings are valid Java.
- A repeated `Declaration` (constructors) is refused when an inherited member type, a foreign name or a short claim of the same name would capture it.

## Contracts

- **K1 (kit authors):** reference every type other than `java.lang` and same-package plan types through structured symbols. A literal FQN supertype such as `new library.Base() {}` is outside the guarantee.
- **K2 (compiling host requirement):** no type visible to the compilation that the plan did not declare (hand-written sources, other source roots, annotation-processor output, split packages) may contribute a top-level type to a plan package whose simple name equals the first package segment of a qualified spelling or a pack-literal root in that package (in practice `java`, `org`, `com` and the plan roots).
- **K3 (kit authors):** authored bodies are closed fragments. They use FQNs or `java.lang` and never rely on pack imports. Authored words count as literal words, and authored text is emitted verbatim after the integrity check.

## Type facts

Facts are `{"types": {"pkg.Type": ["MemberTypeOrField", ...]}}`: for each canonical type, the member-type and non-private field names an extending class would inherit. A known type with no members is an empty array. Over-including is safe, under-including is not. The bundle (`JavaTypeFacts.Bundled`, see `Java/JavaTypeFacts.Bundled.md`) covers `java.lang` and the primitive types. A host adds facts with `new JavaLanguagePack(JavaTypeFacts.Parse(json))`, which unions them with the bundle. A type that is neither declared in the plan nor known to the facts refuses with `STAGE-JAVA-0011`.

## Refusals

| Code | Meaning |
| --- | --- |
| `STAGE-JAVA-0001` | identifier violates the identifier policy |
| `STAGE-JAVA-0002` | malformed Unicode escape |
| `STAGE-JAVA-0003` | private-use code point |
| `STAGE-JAVA-0004` | lone surrogate |
| `STAGE-JAVA-0005` | declaration or imports symbol in a non-Java file |
| `STAGE-JAVA-0006` | spelling touches text that could change how it parses |
| `STAGE-JAVA-0007` | file structure (imports, top-level type, package or import words) |
| `STAGE-JAVA-0008` | symbol declared in more than one file |
| `STAGE-JAVA-0009` | names or packages differ only by case |
| `STAGE-JAVA-0010` | no provably bound spelling (also a captured repeated declaration) |
| `STAGE-JAVA-0011` | external type without a declaration or facts |
| `STAGE-JAVA-0012` | literal or authored name shadows a type the pack relies on |
| `STAGE-JAVA-0013` | type in the default package |
| `STAGE-JAVA-0014` | type name equals a plan package name |
| `STAGE-JAVA-0015` | authored text violates output integrity |
| `STAGE-JAVA-0016` | primitive has no java@1 spelling |

## Versioning

Any change to the bytes the pack produces for the same plan is a new major version of `java@1`. The prerelease `1.0.0-prerelease` carries no stability promise.

## Follow-up

Binding is proven by reasoning, not by a compiler. A javac/javap conformance check over the resolved K1 corpus is a follow-up.
