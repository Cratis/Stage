# Stage language packs

Prerelease, non-packable language packs for the declarative renderer. The first pack is `java@1`.

## Java declaration registry

`JavaPlan.Create(files)` admits all mapped `DeclarativeLanguageFile` values together. Its immutable `Declarations` map is keyed by the original `TemplateSymbol`, with each entry containing the allocated symbol and declaring file. Repeated handles in one file (for example constructors) share that entry. `Files` retains the input order and original segments; emission is a separate step.

A package and every dotted name segment must match `[A-Za-z_][A-Za-z0-9_]*`, excluding `_` and Java reserved or contextual words. Dollar signs are not structured nested-type separators: use `Outer.Inner`. Default-package symbols refuse with `STAGE-JAVA-0013`; invalid segments refuse with `STAGE-JAVA-0001`.

A path ending in ordinal `.java` must contain one imports segment before its first declaration. That declaration must be the top-level type matching the file basename; there must be exactly one distinct top-level symbol, and each nested declaration's immediate prefix must be declared in the same file and package. Decoded literal text before the first declaration cannot supply `package` or `import` words, even in comments. Violations refuse with `STAGE-JAVA-0007`. Other files may contain references, but declaration or imports handles refuse with `STAGE-JAVA-0005`.

Across the plan, duplicate declarations in different files refuse with `STAGE-JAVA-0008`. Packages (including their prefixes) and same-package top-level names that differ only in ordinal case refuse with `STAGE-JAVA-0009`. A top-level type whose canonical name is also a plan package refuses with `STAGE-JAVA-0014`. Top-level names are never renamed.

Nested declarations are compared only with immediate siblings and enclosing names. A name equal to an enclosing name, or siblings differing only in ordinal case, is allocated as `Name_<hash>`. The hash starts with eight lowercase hex digits of SHA-256 over `package + '\n' + original dotted name`. All colliding candidates are extended by four digits at a time, without a visit-order winner, avoiding sibling and enclosing names and every literal or authored word in the file. Beyond the first digest, further digits come from SHA-256 over the same identity plus `'\n' + digitsAlreadyAvailable`. An allocation exceeding 255 ASCII bytes refuses with `STAGE-JAVA-0001`. Descendants always use their parent's allocated prefix.
