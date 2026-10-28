# java@1 binding verification

This JDK 21 harness compiles self-contained java@1 vectors and checks each structured reference against its intended canonical type. It uses the public javac `Trees` API: class files alone cannot preserve the link to a reference site. `javap -v -p` output is also written to the results directory for inspection, not used as the gate.

## Reproduce

Emit the vectors from a Debug build (the target must be absent or empty):

```sh
STAGE_JAVA_BINDING_VECTORS="$PWD/.ai-work/java-bindings/vectors" \
  dotnet test Source/Rendering.Languages/Rendering.Languages.csproj --configuration Debug \
  --filter 'FullyQualifiedName~for_JavaBindingVectors.when_emitting_the_binding_vectors'
docker run --rm -v "$PWD":/w -w /w eclipse-temurin:21-jdk \
  bash Verification/java-bindings/run.sh .ai-work/java-bindings/vectors .ai-work/java-bindings/results
```

Normal .NET builds and specs do not require a JDK. Without the environment variable, the emission spec uses and removes its own temporary directory.

## Vector format v1

Each `positive/` and `negative/` set has `index.tsv`. Its first line is `# java-binding-vectors v1 cases=C files=F sites=S`; subsequent rows contain case name, Java source file count, and reference-site count, separated by tabs. Counts must be nonzero and match disk contents.

Each case contains `src/` with exact resolved plan sources, host sources, and facts-derived stubs. `sites.tsv` rows contain source path relative to `src/`, start, exclusive end, and intended canonical type. Offsets are UTF-16 units, as in javac, into UTF-8 files without a BOM or newline translation. Nested type names use dots. Every non-JDK fact type is stubbed; each fact member becomes both a static field and a static member type to exercise worst-case shadowing and obscuring.

Negative cases also contain `expected.tsv`, with the same four fields followed by the actual type. They deliberately under-include inherited member facts: javac binds `String` to `lib.Base.String` instead of `java.lang.String`. This self-test runs first on every invocation and must detect exactly the planted mismatch set.

## Results and exits

- **0:** all positives compile and every reference binds as intended; prints checked reference, case, and file counts.
- **1:** a positive compile error or binding defect, with file/span/intended/actual diagnostics.
- **2:** verification could not run: missing or inconsistent inputs, unavailable compiler/positions, IO failure, or ineffective negative self-test.

Classes and disassemblies are written under `<results>/<set>/<case>/`. Use a fresh results directory to avoid stale disassembly evidence.

The self-contained catalog includes K1 synthetic goldens, inheritance, anonymous classes, nested references, same-name types, package-head obscuring, and JDK controls. The frozen 289-file K1 corpus is not compiled here: it calls Axon, Spring, and Jakarta APIs that empty member facts cannot stub. Separate .NET specs check its 2,816 recorded reference sites and unchanged output digests without a JDK.
