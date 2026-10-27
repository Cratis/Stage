// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Defines the frozen public top-level java.lang names from JDK 21, including preview types.
/// Adding names from later JDKs changes the java@1 byte contract and requires a new pack version.
/// </summary>
public static class JavaLangTypes
{
    /// <summary>
    /// Gets the case-sensitive JDK 21 names, including ScopedValue and StringTemplate.
    /// </summary>
    public static FrozenSet<string> Names { get; } =
        ("AbstractMethodError Appendable ArithmeticException ArrayIndexOutOfBoundsException ArrayStoreException " +
         "AssertionError AutoCloseable Boolean BootstrapMethodError Byte Character CharSequence Class ClassCastException " +
         "ClassCircularityError ClassFormatError ClassLoader ClassNotFoundException ClassValue CloneNotSupportedException " +
         "Cloneable Comparable Deprecated Double Enum EnumConstantNotPresentException Error Exception " +
         "ExceptionInInitializerError Float FunctionalInterface IllegalAccessError IllegalAccessException " +
         "IllegalArgumentException IllegalCallerException IllegalMonitorStateException IllegalStateException " +
         "IllegalThreadStateException IncompatibleClassChangeError IndexOutOfBoundsException InheritableThreadLocal " +
         "InstantiationError InstantiationException Integer InternalError InterruptedException Iterable LayerInstantiationException " +
         "LinkageError Long MatchException Math Module ModuleLayer NegativeArraySizeException NoClassDefFoundError " +
         "NoSuchFieldError NoSuchFieldException NoSuchMethodError NoSuchMethodException NullPointerException Number " +
         "NumberFormatException Object OutOfMemoryError Override Package Process ProcessBuilder ProcessHandle Readable " +
         "Record ReflectiveOperationException Runnable Runtime RuntimeException RuntimePermission SafeVarargs " +
         "ScopedValue SecurityException SecurityManager Short StackOverflowError StackTraceElement StackWalker StrictMath String " +
         "StringBuffer StringBuilder StringIndexOutOfBoundsException StringTemplate SuppressWarnings System Thread ThreadDeath ThreadGroup " +
         "ThreadLocal Throwable TypeNotPresentException UnknownError UnsatisfiedLinkError UnsupportedClassVersionError " +
         "UnsupportedOperationException VerifyError VirtualMachineError Void WrongThreadException").Split(' ').ToFrozenSet(StringComparer.Ordinal);
}
