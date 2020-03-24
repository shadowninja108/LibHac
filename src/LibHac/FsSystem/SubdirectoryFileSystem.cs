﻿using System;
using System.Diagnostics;
using LibHac.Common;
using LibHac.Fs;

namespace LibHac.FsSystem
{
    public class SubdirectoryFileSystem : FileSystemBase
    {
        private IFileSystem BaseFileSystem { get; }
        private U8String RootPath { get; set; }
        private bool PreserveUnc { get; }

        public static Result CreateNew(out SubdirectoryFileSystem created, IFileSystem baseFileSystem, U8Span rootPath, bool preserveUnc = false)
        {
            var obj = new SubdirectoryFileSystem(baseFileSystem, preserveUnc);
            Result rc = obj.Initialize(rootPath);

            if (rc.IsSuccess())
            {
                created = obj;
                return Result.Success;
            }

            obj.Dispose();
            created = default;
            return rc;
        }

        public SubdirectoryFileSystem(IFileSystem baseFileSystem, bool preserveUnc = false)
        {
            BaseFileSystem = baseFileSystem;
            PreserveUnc = preserveUnc;
        }

        private Result Initialize(U8Span rootPath)
        {
            if (StringUtils.GetLength(rootPath, PathTools.MaxPathLength + 1) > PathTools.MaxPathLength)
                return ResultFs.TooLongPath.Log();

            Span<byte> normalizedPath = stackalloc byte[PathTools.MaxPathLength + 2];

            Result rc = PathTool.Normalize(normalizedPath, out long normalizedPathLen, rootPath, PreserveUnc, false);
            if (rc.IsFailure()) return rc;

            // Ensure a trailing separator
            if (!PathTool.IsSeparator(normalizedPath[(int)normalizedPathLen - 1]))
            {
                Debug.Assert(normalizedPathLen + 2 <= normalizedPath.Length);

                normalizedPath[(int)normalizedPathLen] = StringTraits.DirectorySeparator;
                normalizedPath[(int)normalizedPathLen + 1] = StringTraits.NullTerminator;
                normalizedPathLen++;
            }

            var buffer = new byte[normalizedPathLen + 1];
            normalizedPath.Slice(0, (int)normalizedPathLen).CopyTo(buffer);
            RootPath = new U8String(buffer);

            return Result.Success;
        }

        private Result ResolveFullPath(Span<byte> outPath, U8Span relativePath)
        {
            if (RootPath.Length + StringUtils.GetLength(relativePath, PathTools.MaxPathLength + 1) > outPath.Length)
                return ResultFs.TooLongPath.Log();

            // Copy root path to the output
            RootPath.Value.CopyTo(outPath);

            // Copy the normalized relative path to the output
            return PathTool.Normalize(outPath.Slice(RootPath.Length - 2), out _, relativePath, PreserveUnc, false);
        }

        protected override Result CreateDirectoryImpl(U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.CreateDirectory(new U8Span(fullPath));
        }

        protected override Result CreateFileImpl(U8Span path, long size, CreateFileOptions options)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.CreateFile(new U8Span(fullPath), size, options);
        }

        protected override Result DeleteDirectoryImpl(U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.DeleteDirectory(new U8Span(fullPath));
        }

        protected override Result DeleteDirectoryRecursivelyImpl(U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.DeleteDirectoryRecursively(new U8Span(fullPath));
        }

        protected override Result CleanDirectoryRecursivelyImpl(U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.CleanDirectoryRecursively(new U8Span(fullPath));
        }

        protected override Result DeleteFileImpl(U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.DeleteFile(new U8Span(fullPath));
        }

        protected override Result OpenDirectoryImpl(out IDirectory directory, U8Span path, OpenDirectoryMode mode)
        {
            directory = default;

            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.OpenDirectory(out directory, new U8Span(fullPath), mode);
        }

        protected override Result OpenFileImpl(out IFile file, U8Span path, OpenMode mode)
        {
            file = default;

            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.OpenFile(out file, new U8Span(fullPath), mode);
        }

        protected override Result RenameDirectoryImpl(U8Span oldPath, U8Span newPath)
        {
            Span<byte> fullOldPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Span<byte> fullNewPath = stackalloc byte[PathTools.MaxPathLength + 1];

            Result rc = ResolveFullPath(fullOldPath, oldPath);
            if (rc.IsFailure()) return rc;

            rc = ResolveFullPath(fullNewPath, newPath);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.RenameDirectory(new U8Span(fullOldPath), new U8Span(fullNewPath));
        }

        protected override Result RenameFileImpl(U8Span oldPath, U8Span newPath)
        {
            Span<byte> fullOldPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Span<byte> fullNewPath = stackalloc byte[PathTools.MaxPathLength + 1];

            Result rc = ResolveFullPath(fullOldPath, oldPath);
            if (rc.IsFailure()) return rc;

            rc = ResolveFullPath(fullNewPath, newPath);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.RenameFile(new U8Span(fullOldPath), new U8Span(fullNewPath));
        }

        protected override Result GetEntryTypeImpl(out DirectoryEntryType entryType, U8Span path)
        {
            entryType = default;

            FsPath fullPath;
            unsafe { _ = &fullPath; } // workaround for CS0165

            Result rc = ResolveFullPath(fullPath.Str, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.GetEntryType(out entryType, fullPath);
        }

        protected override Result CommitImpl()
        {
            return BaseFileSystem.Commit();
        }

        protected override Result CommitProvisionallyImpl(long commitCount)
        {
            return BaseFileSystem.CommitProvisionally(commitCount);
        }

        protected override Result RollbackImpl()
        {
            return BaseFileSystem.Rollback();
        }

        protected override Result GetFreeSpaceSizeImpl(out long freeSpace, U8Span path)
        {
            freeSpace = default;

            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.GetFreeSpaceSize(out freeSpace, new U8Span(fullPath));
        }

        protected override Result GetTotalSpaceSizeImpl(out long totalSpace, U8Span path)
        {
            totalSpace = default;

            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.GetTotalSpaceSize(out totalSpace, new U8Span(fullPath));
        }

        protected override Result GetFileTimeStampRawImpl(out FileTimeStampRaw timeStamp, U8Span path)
        {
            timeStamp = default;

            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.GetFileTimeStampRaw(out timeStamp, new U8Span(fullPath));
        }

        protected override Result QueryEntryImpl(Span<byte> outBuffer, ReadOnlySpan<byte> inBuffer, QueryId queryId,
            U8Span path)
        {
            Span<byte> fullPath = stackalloc byte[PathTools.MaxPathLength + 1];
            Result rc = ResolveFullPath(fullPath, path);
            if (rc.IsFailure()) return rc;

            return BaseFileSystem.QueryEntry(outBuffer, inBuffer, queryId, new U8Span(fullPath));
        }
    }
}
