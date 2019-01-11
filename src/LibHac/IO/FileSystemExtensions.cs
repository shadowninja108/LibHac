﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

#if !NETFRAMEWORK
using System.IO.Enumeration;
#endif

namespace LibHac.IO
{
    public static class FileSystemExtensions
    {
        public static void CopyDirectory(this IDirectory source, IDirectory dest, IProgressReport logger = null)
        {
            IFileSystem sourceFs = source.ParentFileSystem;
            IFileSystem destFs = dest.ParentFileSystem;

            foreach (DirectoryEntry entry in source.Read())
            {
                string subSrcPath = PathTools.Normalize(source.FullPath + '/' + entry.Name);
                string subDstPath = PathTools.Normalize(dest.FullPath + '/' + entry.Name);

                if (entry.Type == DirectoryEntryType.Directory)
                {
                    destFs.CreateDirectory(subDstPath);
                    IDirectory subSrcDir = sourceFs.OpenDirectory(subSrcPath, OpenDirectoryMode.All);
                    IDirectory subDstDir = destFs.OpenDirectory(subDstPath, OpenDirectoryMode.All);

                    subSrcDir.CopyDirectory(subDstDir, logger);
                }

                if (entry.Type == DirectoryEntryType.File)
                {
                    destFs.CreateFile(subDstPath, entry.Size);

                    using (IFile srcFile = sourceFs.OpenFile(subSrcPath, OpenMode.Read))
                    using (IFile dstFile = destFs.OpenFile(subDstPath, OpenMode.Write))
                    {
                        logger?.LogMessage(subSrcPath);
                        srcFile.CopyTo(dstFile, logger);
                    }
                }
            }
        }

        public static void CopyFileSystem(this IFileSystem source, IFileSystem dest, IProgressReport logger = null)
        {
            IDirectory sourceRoot = source.OpenDirectory("/", OpenDirectoryMode.All);
            IDirectory destRoot = dest.OpenDirectory("/", OpenDirectoryMode.All);

            sourceRoot.CopyDirectory(destRoot, logger);
        }

        public static void Extract(this IFileSystem source, string destinationPath, IProgressReport logger = null)
        {
            var destFs = new LocalFileSystem(destinationPath);

            source.CopyFileSystem(destFs, logger);
        }

        public static IEnumerable<DirectoryEntry> EnumerateEntries(this IFileSystem fileSystem)
        {
            return fileSystem.OpenDirectory("/", OpenDirectoryMode.All).EnumerateEntries();
        }

        public static IEnumerable<DirectoryEntry> EnumerateEntries(this IDirectory directory)
        {
            return directory.EnumerateEntries("*", SearchOptions.Default);
        }

        public static IEnumerable<DirectoryEntry> EnumerateEntries(this IDirectory directory, string searchPattern, SearchOptions searchOptions)
        {
            bool ignoreCase = searchOptions.HasFlag(SearchOptions.CaseInsensitive);
            bool recurse = searchOptions.HasFlag(SearchOptions.RecurseSubdirectories);

            IFileSystem fs = directory.ParentFileSystem;

            foreach (DirectoryEntry entry in directory.Read())
            {
                if (MatchesPattern(searchPattern, entry.Name, ignoreCase))
                {
                    yield return entry;
                }

                if (entry.Type != DirectoryEntryType.Directory || !recurse) continue;

                IDirectory subDir = fs.OpenDirectory(directory.FullPath + '/' + entry.Name, OpenDirectoryMode.All);

                foreach (DirectoryEntry subEntry in subDir.EnumerateEntries(searchPattern, searchOptions))
                {
                    yield return subEntry;
                }
            }
        }

        public static void CopyTo(this IFile file, IFile dest, IProgressReport logger = null)
        {
            const int bufferSize = 0x8000;
            logger?.SetTotal(file.GetSize());

            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                long inOffset = 0;

                int bytesRead;
                while ((bytesRead = file.Read(buffer, inOffset)) != 0)
                {
                    dest.Write(buffer.AsSpan(0, bytesRead), inOffset);
                    inOffset += bytesRead;
                    logger?.ReportAdd(bytesRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                logger?.SetTotal(0);
            }
        }

        public static Stream AsStream(this IFile file) => new NxFileStream(file, true);
        public static Stream AsStream(this IFile file, bool keepOpen) => new NxFileStream(file, keepOpen);

        public static IFile AsIFile(this Stream stream, OpenMode mode) => new StreamFile(stream, mode);

        public static int GetEntryCount(this IFileSystem fs, OpenDirectoryMode mode)
        {
            return fs.OpenDirectory("/", OpenDirectoryMode.All).GetEntryCountRecursive(mode);
        }

        public static int GetEntryCountRecursive(this IDirectory directory, OpenDirectoryMode mode)
        {
            int count = 0;

            foreach (DirectoryEntry entry in directory.EnumerateEntries())
            {
                if (entry.Type == DirectoryEntryType.Directory && (mode & OpenDirectoryMode.Directories) != 0 ||
                    entry.Type == DirectoryEntryType.File && (mode & OpenDirectoryMode.Files) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool MatchesPattern(string searchPattern, string name, bool ignoreCase)
        {
#if NETFRAMEWORK
            return Compatibility.FileSystemName.MatchesSimpleExpression(searchPattern.AsSpan(),
                name.AsSpan(), ignoreCase);
#else
            return FileSystemName.MatchesSimpleExpression(searchPattern.AsSpan(),
                name.AsSpan(), ignoreCase);
#endif
        }
    }

    [Flags]
    public enum SearchOptions
    {
        Default = 0,
        RecurseSubdirectories = 1 << 0,
        CaseInsensitive = 1 << 1
    }
}
