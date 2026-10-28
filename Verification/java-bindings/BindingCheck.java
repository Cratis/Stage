// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import com.sun.source.tree.CompilationUnitTree;
import com.sun.source.tree.IdentifierTree;
import com.sun.source.tree.MemberSelectTree;
import com.sun.source.tree.Tree;
import com.sun.source.util.JavacTask;
import com.sun.source.util.SourcePositions;
import com.sun.source.util.TreePath;
import com.sun.source.util.TreePathScanner;
import com.sun.source.util.Trees;
import java.io.IOException;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.regex.Pattern;
import javax.lang.model.element.Element;
import javax.lang.model.element.TypeElement;
import javax.tools.Diagnostic;
import javax.tools.DiagnosticCollector;
import javax.tools.JavaFileObject;
import javax.tools.ToolProvider;

/** Checks emitted reference sites with the JDK compiler, without external dependencies. */
public final class BindingCheck {
    private static final Pattern HEADER = Pattern.compile("# java-binding-vectors v1 cases=(\\d+) files=(\\d+) sites=(\\d+)");

    private record Span(long start, long end) { }
    private record Site(String path, int start, int end, String intended) { }
    private record Mismatch(String path, int start, int end, String intended, String actual) { }
    private record BindingCase(String name, Path root, List<Path> sources, List<Site> sites) { }
    private record CaseResult(List<Mismatch> mismatches, int errors) { }

    /**
     * Runs the negative self-test, then the positive binding cases.
     * @param args vector root and results directory
     */
    // 0: clean; 1: positive defects; 2: could not run, including an ineffective planted self-test.
    public static void main(String[] args) {
        try {
            if (args.length != 2) {
                throw new IllegalArgumentException("usage: java BindingCheck.java <vectors-root> <results-dir>");
            }
            System.exit(run(Path.of(args[0]).toAbsolutePath().normalize(), Path.of(args[1]).toAbsolutePath().normalize()));
        } catch (Exception failure) {
            System.err.println("could not run: " + failure);
            System.exit(2);
        }
    }

    private static int run(Path vectors, Path results) throws IOException {
        int planted = 0;
        // Under-included facts deliberately let inherited lib.Base.String shadow java.lang.String.
        for (var item : readSet(vectors.resolve("negative"))) {
            var expected = readExpected(item);
            var actual = check(item, results.resolve("negative").resolve(item.name()));
            if (!matchesExpected(actual, expected)) {
                throw new IllegalStateException("self-test " + item.name() + " expected=" + expected + " actual=" + actual);
            }
            planted += expected.size();
        }
        System.out.println("self-test: " + planted + " planted mismatch(es) detected");
        var positive = readSet(vectors.resolve("positive"));
        int defects = 0;
        for (var item : positive) {
            var result = check(item, results.resolve("positive").resolve(item.name()));
            defects += result.errors() + result.mismatches().size();
        }
        if (defects != 0) {
            return 1;
        }
        reportSummary(positive);
        return 0;
    }

    private static void reportSummary(List<BindingCase> cases) {
        System.out.printf("checked %d references in %d cases (%d files)%n", cases.stream().mapToInt(item -> item.sites().size()).sum(), cases.size(), cases.stream().mapToInt(item -> item.sources().size()).sum());
    }

    private static List<BindingCase> readSet(Path root) throws IOException {
        var lines = Files.readAllLines(root.resolve("index.tsv"), StandardCharsets.UTF_8);
        if (lines.isEmpty()) {
            throw new IllegalArgumentException("empty index: " + root);
        }
        var header = HEADER.matcher(lines.getFirst());
        if (!header.matches()) {
            throw new IllegalArgumentException("invalid index header: " + root);
        }
        var cases = new ArrayList<BindingCase>();
        var names = new HashSet<String>();
        for (var line : lines.subList(1, lines.size())) {
            var row = columns(line, 3);
            registerCase(names, row[0]);
            cases.add(readCase(root, row));
        }
        requireCount(cases.size(), header.group(1), root + " cases");
        requireCount(cases.stream().mapToInt(item -> item.sources().size()).sum(), header.group(2), root + " files");
        requireCount(cases.stream().mapToInt(item -> item.sites().size()).sum(), header.group(3), root + " sites");
        checkDirectories(root, names);
        return cases;
    }

    private static void registerCase(Set<String> names, String name) {
        if (!names.add(name) || name.contains("/")) {
            throw new IllegalArgumentException("duplicate or invalid case: " + name);
        }
    }

    private static BindingCase readCase(Path root, String[] row) throws IOException {
        var folder = child(root, row[0]);
        List<Path> sources;
        try (var paths = Files.walk(folder.resolve("src"))) {
            sources = paths.filter(Files::isRegularFile).filter(path -> path.toString().endsWith(".java")).sorted().toList();
        }
        var sites = readSites(folder);
        requireCount(sources.size(), row[1], folder + " files");
        requireCount(sites.size(), row[2], folder + " sites");
        return new BindingCase(row[0], folder, sources, sites);
    }

    private static void checkDirectories(Path root, Set<String> names) throws IOException {
        try (var folders = Files.list(root)) {
            var onDisk = folders.filter(Files::isDirectory).map(path -> path.getFileName().toString()).collect(java.util.stream.Collectors.toSet());
            if (!onDisk.equals(names)) {
                throw new IllegalArgumentException("case directories differ from index: " + root);
            }
        }
    }

    private static boolean matchesExpected(CaseResult result, Set<Mismatch> expected) {
        return result.errors() == 0 && result.mismatches().size() == expected.size() && new HashSet<>(result.mismatches()).equals(expected);
    }

    private static List<Site> readSites(Path folder) throws IOException {
        var sites = new ArrayList<Site>();
        for (var line : Files.readAllLines(folder.resolve("sites.tsv"), StandardCharsets.UTF_8)) {
            sites.add(readSite(folder, line));
        }
        if (new HashSet<>(sites).size() != sites.size()) {
            throw new IllegalArgumentException("duplicate sites: " + folder);
        }
        return sites;
    }

    private static Site readSite(Path folder, String line) throws IOException {
        var row = columns(line, 4);
        var site = new Site(row[0], Integer.parseInt(row[1]), Integer.parseInt(row[2]), row[3]);
        int length = Files.readString(child(folder.resolve("src"), site.path()), StandardCharsets.UTF_8).length();
        if (!validSite(site, length)) {
            throw new IllegalArgumentException("invalid site: " + folder + "/" + line);
        }
        return site;
    }

    private static boolean validSite(Site site, int length) {
        return site.path().endsWith(".java") && site.start() >= 0 && site.end() > site.start() && site.end() <= length && !site.intended().isBlank();
    }

    private static Set<Mismatch> readExpected(BindingCase item) throws IOException {
        var expected = new HashSet<Mismatch>();
        for (var line : Files.readAllLines(item.root().resolve("expected.tsv"), StandardCharsets.UTF_8)) {
            var row = columns(line, 5);
            var mismatch = new Mismatch(row[0], Integer.parseInt(row[1]), Integer.parseInt(row[2]), row[3], row[4]);
            if (!expected.add(mismatch) || !item.sites().contains(new Site(mismatch.path(), mismatch.start(), mismatch.end(), mismatch.intended()))) {
                throw new IllegalArgumentException("invalid planted mismatch: " + line);
            }
        }
        if (expected.isEmpty()) {
            throw new IllegalArgumentException("empty planted mismatch set: " + item.name());
        }
        return expected;
    }

    private static CaseResult check(BindingCase item, Path results) throws IOException {
        var compiler = ToolProvider.getSystemJavaCompiler();
        if (compiler == null) {
            throw new IllegalStateException("a JDK 21 compiler is required");
        }
        var diagnostics = new DiagnosticCollector<JavaFileObject>();
        var mismatches = new ArrayList<Mismatch>();
        var classes = results.resolve("classes");
        Files.createDirectories(classes);
        try (var files = compiler.getStandardFileManager(diagnostics, Locale.ROOT, StandardCharsets.UTF_8)) {
            var task = (JavacTask) compiler.getTask(null, files, diagnostics,
                List.of("--release", "21", "-proc:none", "-g", "-implicit:none", "-encoding", "UTF-8", "-d", classes.toString()), null, files.getJavaFileObjectsFromPaths(item.sources()));
            var trees = Trees.instance(task);
            var paths = parsedPaths(task, trees);
            task.analyze();
            if (compileErrors(diagnostics).isEmpty()) {
                checkSites(item, paths, trees, mismatches);
                task.generate();
            }
        }
        var errors = compileErrors(diagnostics);
        for (var diagnostic : errors) {
            System.out.printf("COMPILE %s %s:%d:%d %s%n", item.name(), diagnostic.getSource() == null ? "none" : diagnostic.getSource().getName(), diagnostic.getLineNumber(), diagnostic.getColumnNumber(), diagnostic.getMessage(Locale.ROOT));
        }
        if (errors.isEmpty()) {
            disassemble(classes, results.resolve("javap"));
        }
        return new CaseResult(mismatches, errors.size());
    }

    private static Map<Path, Map<Span, List<TreePath>>> parsedPaths(JavacTask task, Trees trees) throws IOException {
        var paths = new HashMap<Path, Map<Span, List<TreePath>>>();
        // Capture parsed source trees before attribution adds synthetic nodes without positions.
        for (var unit : task.parse()) {
            paths.put(Path.of(unit.getSourceFile().toUri()).toAbsolutePath().normalize(), sourcePaths(trees, unit));
        }
        return paths;
    }

    private static List<Diagnostic<? extends JavaFileObject>> compileErrors(DiagnosticCollector<JavaFileObject> diagnostics) {
        return diagnostics.getDiagnostics().stream().filter(diagnostic -> diagnostic.getKind() == Diagnostic.Kind.ERROR).toList();
    }

    private static void checkSites(BindingCase item, Map<Path, Map<Span, List<TreePath>>> paths, Trees trees, List<Mismatch> mismatches) {
        for (var site : item.sites()) {
            var candidates = paths.get(child(item.root().resolve("src"), site.path())).getOrDefault(new Span(site.start(), site.end()), List.of());
            String actual = "none";
            for (var candidate : candidates) {
                Element element = trees.getElement(candidate);
                actual = elementName(element);
                if (element instanceof TypeElement && actual.equals(site.intended())) {
                    break;
                }
            }
            if (!actual.equals(site.intended())) {
                var mismatch = new Mismatch(site.path(), site.start(), site.end(), site.intended(), actual);
                mismatches.add(mismatch);
                System.out.printf("MISMATCH %s/%s:%d-%d intended=%s actual=%s%n", item.name(), site.path(), site.start(), site.end(), site.intended(), actual);
            }
        }
    }

    private static String elementName(Element element) {
        if (element instanceof TypeElement type) {
            return type.getQualifiedName().toString();
        }
        return element == null ? "none" : element.getKind() + ":" + element;
    }

    private static Map<Span, List<TreePath>> sourcePaths(Trees trees, CompilationUnitTree unit) {
        var paths = new HashMap<Span, List<TreePath>>();
        SourcePositions positions = trees.getSourcePositions();
        new TreePathScanner<Void, Void>() {
            private void record(Tree tree) {
                long start = positions.getStartPosition(unit, tree);
                long end = positions.getEndPosition(unit, tree);
                if (start == Diagnostic.NOPOS || end == Diagnostic.NOPOS) {
                    throw new IllegalStateException("NOPOS in " + unit.getSourceFile().getName() + ": " + tree);
                }
                paths.computeIfAbsent(new Span(start, end), ignored -> new ArrayList<>()).add(getCurrentPath());
            }
            @Override public Void visitIdentifier(IdentifierTree tree, Void ignored) {
                record(tree);
                return super.visitIdentifier(tree, ignored);
            }
            @Override public Void visitMemberSelect(MemberSelectTree tree, Void ignored) {
                record(tree);
                return super.visitMemberSelect(tree, ignored);
            }
        }.scan(unit, null);
        return paths;
    }

    private static void disassemble(Path classes, Path results) throws IOException {
        var javap = java.util.spi.ToolProvider.findFirst("javap").orElseThrow(() -> new IllegalStateException("javap is missing"));
        Files.createDirectories(results);
        try (var paths = Files.walk(classes)) {
            for (var file : paths.filter(path -> path.toString().endsWith(".class")).sorted(Comparator.naturalOrder()).toList()) {
                String name = classes.relativize(file).toString().replace(file.getFileSystem().getSeparator(), ".").replaceFirst("\\.class$", "");
                try (var out = new PrintWriter(Files.newBufferedWriter(results.resolve(name + ".txt"), StandardCharsets.UTF_8))) {
                    if (javap.run(out, out, "-v", "-p", "-classpath", classes.toString(), name) != 0) {
                        throw new IllegalStateException("javap failed: " + name);
                    }
                }
            }
        }
    }

    private static Path child(Path root, String relative) {
        var path = Path.of(relative);
        if (!safePath(root, relative, path)) {
            throw new IllegalArgumentException("unsafe vector path: " + relative);
        }
        return root.resolve(path).normalize();
    }

    private static boolean safePath(Path root, String relative, Path path) {
        return !relative.isBlank() && !path.isAbsolute() && !relative.contains("\\") && root.resolve(path).normalize().startsWith(root) && !path.normalize().toString().equals(".");
    }

    private static String[] columns(String line, int count) {
        var columns = line.split("\t", -1);
        if (columns.length != count) {
            throw new IllegalArgumentException("expected " + count + " TSV columns: " + line);
        }
        return columns;
    }

    private static void requireCount(int actual, String expected, String subject) {
        if (actual <= 0 || actual != Integer.parseInt(expected)) {
            throw new IllegalArgumentException(subject + " expected=" + expected + " actual=" + actual);
        }
    }
}
