using System;
using System.Buffers;
using System.IO;

#if !STREAM_SPAN
using System.Buffers;
#endif

namespace LibHac.Fs
{
    /// <summary>
    /// Provides an <see cref="IFile"/> interface for interacting with a <see cref="Stream"/>
    /// </summary>
    public class StreamFile : FileBase
    {
        private Stream BaseStream { get; }
        private object Locker { get; } = new object();

        public StreamFile(Stream baseStream, OpenMode mode)
        {
            BaseStream = baseStream;
            Mode = mode;
        }

        public override int Read(Span<byte> destination, long offset, ReadOption options)
        {

            byte[] buffer = ArrayPool<byte>.Shared.Rent(destination.Length);
            try
            {
                int bytesRead;
                lock (Locker)
                {
                    if (BaseStream.Position != offset)
                    {
                        BaseStream.Position = offset;
                    }

                    bytesRead = BaseStream.Read(buffer, 0, destination.Length);
                }

                new Span<byte>(buffer, 0, destination.Length).CopyTo(destination);

                return bytesRead;
            } catch(IOException e)
            {
                if (!e.Message.Contains("Incorrect function"))
                    throw e;

                return Read(destination, offset, options);
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        public override void Write(ReadOnlySpan<byte> source, long offset, WriteOption options)
        {
#if STREAM_SPAN
            lock (Locker)
            {
                BaseStream.Position = offset;
                BaseStream.Write(source);
            }
#else
            byte[] buffer = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                source.CopyTo(buffer);

                lock (Locker)
                {
                    BaseStream.Position = offset;
                    BaseStream.Write(buffer, 0, source.Length);
                }
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
#endif

            if ((options & WriteOption.Flush) != 0)
            {
                Flush();
            }
        }

        public override void Flush()
        {
            lock (Locker)
            {
                BaseStream.Flush();
            }
        }

        public override long GetSize()
        {
            lock (Locker)
            {
                return BaseStream.Length;
            }
        }

        public override void SetSize(long size)
        {
            lock (Locker)
            {
                BaseStream.SetLength(size);
            }
        }
    }
}
