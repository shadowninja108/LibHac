﻿using System;
using LibHac.Fs;

namespace LibHac.FsSystem
{
    public class NullFile : FileBase
    {
        private OpenMode Mode { get; }

        public NullFile()
        {
            Mode = OpenMode.ReadWrite;
        }

        public NullFile(long length) : this() => Length = length;

        private long Length { get; }

        protected override Result ReadImpl(out long bytesRead, long offset, Span<byte> destination, ReadOption options)
        {
            bytesRead = 0;

            Result rc = ValidateReadParams(out long toRead, offset, destination.Length, Mode);
            if (rc.IsFailure()) return rc;

            destination.Slice(0, (int)toRead).Clear();

            bytesRead = toRead;
            return Result.Success;
        }

        protected override Result WriteImpl(long offset, ReadOnlySpan<byte> source, WriteOption options)
        {
            return Result.Success;
        }

        protected override Result FlushImpl()
        {
            return Result.Success;
        }

        protected override Result GetSizeImpl(out long size)
        {
            size = Length;
            return Result.Success;
        }

        protected override Result SetSizeImpl(long size)
        {
            return ResultFs.UnsupportedOperation.Log();
        }
    }
}
