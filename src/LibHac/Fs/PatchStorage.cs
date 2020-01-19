using System;
using System.Collections.Generic;
using System.Linq;
using static LibHac.Util;

namespace LibHac.Fs
{
    public class PatchStorage : StorageBase
    {
        public IStorage BaseStorage;

        public Dictionary<Range, byte[]> Patches;

        public PatchStorage(IStorage baseStorage)
        {
            BaseStorage = baseStorage;
            Patches = new Dictionary<Range, byte[]>();
        }

        protected override Result FlushImpl() => BaseStorage.Flush();
        protected override Result GetSizeImpl(out long size)
        {
            Result rc = BaseStorage.GetSize(out long localSize);
            size = localSize;
            return rc;
        }

        public void AddPatch(Span<byte> data, long offset)
        {
            Range dataRange = new Range(offset, offset + data.Length);
            IEnumerable<Range> overlapping = Patches.Keys.Overlapping(dataRange).OrderByDescending(r => r.Length);

            if (overlapping.Any()) // if the data overlaps with an existing patch, we need to merge it
            {
                Range mergedRange = new Range(overlapping.Min(r => r.Start), overlapping.Max(r => r.End));
                byte[] mergedData = new byte[mergedRange.Length];

                // copy data into new range (largest to smallest)
                foreach(Range r in overlapping.Concat(new Range[] { dataRange }))
                {
                    byte[] iData = Patches[r];
                    Array.Copy(iData, 0, mergedData, r.Start - mergedRange.Start, iData.Length);
                }

                Patches.RemoveAll(overlapping);
                Patches[mergedRange] = mergedData;
            }
            else
            {
                Patches[dataRange] = data.ToArray();
            }
        }

        protected override Result ReadImpl(long offset, Span<byte> destination)
        {
            IEnumerable<Range> overlapping = Patches.Keys.Containing(offset);
            foreach (Range r in overlapping)
            {
                int relativeStart = (int) (offset - r.Start);
                int relativeEnd = (int)(r.Length-1);
                new Span<byte>(Patches[r], relativeStart, relativeEnd - relativeStart).CopyTo(destination.Slice(relativeStart));
            }
            foreach (Range r in overlapping.Mask())
                BaseStorage.Read(r.Start, destination.Slice((int)(r.Start - offset), (int)r.Length));

            return Result.Success;
        }

        protected override Result WriteImpl(long offset, ReadOnlySpan<byte> source)
        {
            Span<byte> data = new byte[source.Length];
            source.CopyTo(data);
            AddPatch(data, offset);
            return Result.Success;
        }

        protected override Result SetSizeImpl(long size) => BaseStorage.SetSize(size);
    }
}
