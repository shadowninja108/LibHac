using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibHac
{
    public class Range
    {
        public long Start, End;
        public long Length => End - Start;

        public Range(long start, long end)
        {
            Start = start;
            End = end;
        }

        public bool Contains(long t)
        {
            return (Start <= t) && (t <= End);
        }

        public bool Overlaps(Range r)
        {
            return Start < r.End && r.Start < End;
        }

        public bool Contains(Range r)
        {
            return Start <= r.Start && r.End <= End;
        }

        public Range Combine(Range r)
        {
            if (!Overlaps(r))
                return null;
            return new Range(Math.Min(r.Start, Start), Math.Max(r.End, End));
        }

        public void Shift(long t)
        {
            Start += t;
            End += t;
        }

        public override bool Equals(object obj)
        {
            if(obj is Range robj)
            {
                return robj.Start == Start && robj.End == End;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)(Start ^ End);
        }
    }
    public static class RangeExtensions
    {
        public static IEnumerable<Range> Containing(this IEnumerable<Range> ranges, long t)
        {
            return ranges.Where(r => r.Contains(t));
        }

        public static IEnumerable<Range> Overlapping(this IEnumerable<Range> ranges, Range t)
        {
            return ranges.Where(r => r.Overlaps(t));
        }

        public static IEnumerable<Range> Merge(this IEnumerable<Range> ranges)
        {
            IEnumerable<Range> enu = new List<Range>();
            foreach (Range r in ranges)
            {
                IEnumerable<Range> next = ranges
                    .Except(new Range[] { r }) // exclude current 
                    .Select(x => x?.Combine(r)) // combine
                    .Where(x => x != null); // exclude non-overlapping
                if (!next.Any()) // range is unique
                    enu = enu.Concat(new Range[] { r });
                else
                    enu = enu.Concat(next);
            }
            enu = enu.Distinct();
            if (enu.Except(ranges).Any()) // was it cut down at all?
                return enu.Merge();
            return enu;
        }

        public static IEnumerable<Range> Mask(this IEnumerable<Range> ranges)
        {
            IEnumerable<Range> merged = ranges.Merge().OrderBy(r => r.Start);
            for(int i = 1; i < merged.Count(); i++)
            {
                Range r1 = merged.Skip(i - 1).First();
                Range r2 = merged.Skip(i).First();
                yield return new Range(r1.End, r2.Start);
            }

        }
    }
}
