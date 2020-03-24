﻿using LibHac.Common;
using LibHac.Fs;
using Xunit;

namespace LibHac.Tests.Fs.IFileSystemTestBase
{
    public abstract partial class IFileSystemTests
    {
        [Fact]
        public void DeleteFile_DoesNotExist_ReturnsPathNotFound()
        {
            IFileSystem fs = CreateFileSystem();

            Result rc = fs.DeleteFile("/file".ToU8Span());
            Assert.Equal(ResultFs.PathNotFound.Value, rc);
        }

        [Fact]
        public void DeleteFile_FileExists_FileEntryIsRemoved()
        {
            IFileSystem fs = CreateFileSystem();

            fs.CreateFile("/file".ToU8Span(), 0, CreateFileOptions.None);

            Result rcDelete = fs.DeleteFile("/file".ToU8Span());
            Result rcEntry = fs.GetEntryType(out _, "/file".ToU8Span());

            Assert.True(rcDelete.IsSuccess());
            Assert.Equal(ResultFs.PathNotFound.Value, rcEntry);
        }

        [Fact]
        public void DeleteFile_PathIsDirectory_ReturnsPathNotFound()
        {
            IFileSystem fs = CreateFileSystem();

            fs.CreateDirectory("/dir".ToU8Span());

            Result rc = fs.DeleteFile("/dir".ToU8Span());

            Assert.Equal(ResultFs.PathNotFound.Value, rc);
        }

        [Fact]
        public void DeleteFile_HasOlderSibling_SiblingNotDeleted()
        {
            IFileSystem fs = CreateFileSystem();

            fs.CreateFile("/file1".ToU8Span(), 0, CreateFileOptions.None);
            fs.CreateFile("/file2".ToU8Span(), 0, CreateFileOptions.None);

            Result rcDelete = fs.DeleteFile("/file2".ToU8Span());
            Result rcEntry1 = fs.GetEntryType(out DirectoryEntryType dir1Type, "/file1".ToU8Span());
            Result rcEntry2 = fs.GetEntryType(out _, "/file2".ToU8Span());

            Assert.True(rcDelete.IsSuccess());
            Assert.True(rcEntry1.IsSuccess());
            Assert.Equal(ResultFs.PathNotFound.Value, rcEntry2);

            Assert.Equal(DirectoryEntryType.File, dir1Type);
        }

        [Fact]
        public void DeleteFile_HasYoungerSibling_SiblingNotDeleted()
        {
            IFileSystem fs = CreateFileSystem();

            fs.CreateFile("/file2".ToU8Span(), 0, CreateFileOptions.None);
            fs.CreateFile("/file1".ToU8Span(), 0, CreateFileOptions.None);

            Result rcDelete = fs.DeleteFile("/file2".ToU8Span());
            Result rcEntry1 = fs.GetEntryType(out DirectoryEntryType dir1Type, "/file1".ToU8Span());
            Result rcEntry2 = fs.GetEntryType(out _, "/file2".ToU8Span());

            Assert.True(rcDelete.IsSuccess());
            Assert.True(rcEntry1.IsSuccess());
            Assert.Equal(ResultFs.PathNotFound.Value, rcEntry2);

            Assert.Equal(DirectoryEntryType.File, dir1Type);
        }
    }
}