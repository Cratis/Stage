// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Lists the public top-level types of <c language="java">java.lang</c>.
/// </summary>
/// <remarks>
/// The list is the union of the JDK 21 and JDK 25 package summaries, so a name that either release implicitly
/// imports is treated as taken. Sources, accessed 2026-10-28:
/// https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/lang/package-summary.html and
/// https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/package-summary.html.
/// JDK 22 through 25 add IO and StableValue; StringTemplate exists only in JDK 21 (preview).
/// </remarks>
public static class JavaLangTypes
{
    /// <summary>
    /// Gets the case-sensitive union of the JDK 21 and JDK 25 names.
    /// </summary>
    public static FrozenSet<string> Names { get; } =
        (
         "AbstractMethodError Appendable ArithmeticException ArrayIndexOutOfBoundsException ArrayStoreException " +
         "AssertionError AutoCloseable Boolean BootstrapMethodError Byte CharSequence Character Class " +
         "ClassCastException ClassCircularityError ClassFormatError ClassLoader ClassNotFoundException " +
         "ClassValue CloneNotSupportedException Cloneable Comparable Deprecated Double Enum " +
         "EnumConstantNotPresentException Error Exception ExceptionInInitializerError Float FunctionalInterface " +
         "IO IllegalAccessError IllegalAccessException IllegalArgumentException IllegalCallerException " +
         "IllegalMonitorStateException IllegalStateException IllegalThreadStateException " +
         "IncompatibleClassChangeError IndexOutOfBoundsException InheritableThreadLocal InstantiationError " +
         "InstantiationException Integer InternalError InterruptedException Iterable " +
         "LayerInstantiationException LinkageError Long MatchException Math Module ModuleLayer " +
         "NegativeArraySizeException NoClassDefFoundError NoSuchFieldError NoSuchFieldException " +
         "NoSuchMethodError NoSuchMethodException NullPointerException Number NumberFormatException Object " +
         "OutOfMemoryError Override Package Process ProcessBuilder ProcessHandle Readable Record " +
         "ReflectiveOperationException Runnable Runtime RuntimeException RuntimePermission SafeVarargs " +
         "ScopedValue SecurityException SecurityManager Short StableValue StackOverflowError StackTraceElement " +
         "StackWalker StrictMath String StringBuffer StringBuilder StringIndexOutOfBoundsException " +
         "StringTemplate SuppressWarnings System Thread ThreadDeath ThreadGroup ThreadLocal Throwable " +
         "TypeNotPresentException UnknownError UnsatisfiedLinkError UnsupportedClassVersionError " +
         "UnsupportedOperationException VerifyError VirtualMachineError Void WrongThreadException"
        ).Split(' ').ToFrozenSet(StringComparer.Ordinal);
}
