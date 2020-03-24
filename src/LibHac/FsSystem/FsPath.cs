﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibHac.Common;
using LibHac.Fs;

namespace LibHac.FsSystem
{
    [DebuggerDisplay("{ToString()}")]
    [StructLayout(LayoutKind.Sequential, Size = MaxLength + 1)]
    public struct FsPath
    {
        internal const int MaxLength = 0x300;

#if DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Padding100 Padding000;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Padding100 Padding100;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Padding100 Padding200;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly byte Padding300;
#endif

        public Span<byte> Str => SpanHelpers.AsByteSpan(ref this);

        public static Result FromSpan(out FsPath fsPath, ReadOnlySpan<byte> path)
        {
            fsPath = new FsPath();

            U8StringBuilder builder = new U8StringBuilder(fsPath.Str).Append(path);

            return builder.Overflowed ? ResultFs.TooLongPath.Log() : Result.Success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator U8Span(in FsPath value) => new U8Span(SpanHelpers.AsReadOnlyByteSpan(ref Unsafe.AsRef(in value)));

        public override string ToString() => StringUtils.Utf8ZToString(Str);
    }
}
