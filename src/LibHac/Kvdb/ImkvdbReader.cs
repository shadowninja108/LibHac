﻿using System;
using System.Runtime.CompilerServices;

namespace LibHac.Kvdb
{
    public ref struct ImkvdbReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _position;

        public ImkvdbReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _position = 0;
        }

        public Result ReadHeader(out int entryCount)
        {
            entryCount = default;

            if (_position + Unsafe.SizeOf<ImkvdbHeader>() > _data.Length)
                return ResultKvdb.InvalidKeyValue.Log();

            ref ImkvdbHeader header = ref Unsafe.As<byte, ImkvdbHeader>(ref Unsafe.AsRef(_data[_position]));

            if (header.Magic != ImkvdbHeader.ExpectedMagic)
            {
                return ResultKvdb.InvalidKeyValue.Log();
            }

            entryCount = header.EntryCount;
            _position += Unsafe.SizeOf<ImkvdbHeader>();

            return Result.Success;
        }

        public Result GetEntrySize(out int keySize, out int valueSize)
        {
            keySize = default;
            valueSize = default;

            if (_position + Unsafe.SizeOf<ImkvdbHeader>() > _data.Length)
                return ResultKvdb.InvalidKeyValue.Log();

            ref ImkvdbEntryHeader header = ref Unsafe.As<byte, ImkvdbEntryHeader>(ref Unsafe.AsRef(_data[_position]));

            if (header.Magic != ImkvdbEntryHeader.ExpectedMagic)
            {
                return ResultKvdb.InvalidKeyValue.Log();
            }

            keySize = header.KeySize;
            valueSize = header.ValueSize;

            return Result.Success;
        }

        public Result ReadEntry(out ReadOnlySpan<byte> key, out ReadOnlySpan<byte> value)
        {
            key = default;
            value = default;

            Result rc = GetEntrySize(out int keySize, out int valueSize);
            if (rc.IsFailure()) return rc;

            _position += Unsafe.SizeOf<ImkvdbEntryHeader>();

            if (_position + keySize + valueSize > _data.Length)
                return ResultKvdb.InvalidKeyValue.Log();

            key = _data.Slice(_position, keySize);
            value = _data.Slice(_position + keySize, valueSize);

            _position += keySize + valueSize;

            return Result.Success;
        }
    }
}
