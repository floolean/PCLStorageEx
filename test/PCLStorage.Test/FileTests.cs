﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
#if PORTABLE
using PCLStorage.Exceptions;
#endif
using PCLStorage.TestFramework;


namespace PCLStorage.Test
{
	[TestClass]
    public class FileTests
    {
		[TestMethod]
		public async Task GetFileThrowsWhenFileDoesNotExist()
		{
			string fileName = Guid.NewGuid().ToString();
			IFolder folder = Storage.AppLocalStorage;
			await ExceptionAssert.ThrowsAsync<FileNotFoundException>(async () => await folder.GetFileAsync(fileName));
		}

        [TestMethod]
        public async Task CreateFile()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "fileToCreate.txt";

            //  Act
            IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Assert
            Assert.AreEqual(fileName, file.Name);
            Assert.AreEqual(PortablePath.Combine(folder.Path, fileName), file.Path, "File Path");

            //  Cleanup
            await file.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFileSubFolder()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string subFolderName = "CreateFileSubFolder";
            IFolder subFolder = await folder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);
            string fileName = "fileToCreateInSubFolder.txt";

            //  Act
            IFile file = await subFolder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Assert
            Assert.AreEqual(fileName, file.Name);
            Assert.AreEqual(PortablePath.Combine(folder.Path, subFolderName, fileName), file.Path, "File Path");

            //  Cleanup
            await file.DeleteAsync();
            await subFolder.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFileNameCollision_GenerateUniqueName()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string baseFileName = "Collision_Unique";
            IFile file1 = await folder.CreateFileAsync(baseFileName + ".txt", CreationCollisionOption.FailIfExists);

            //  Act
            IFile file2 = await folder.CreateFileAsync(baseFileName + ".txt", CreationCollisionOption.GenerateUniqueName);

            //  Assert
            Assert.AreEqual(baseFileName + " (2).txt", file2.Name);

            //  Cleanup
            await file1.DeleteAsync();
            await file2.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFileNameCollision_ReplaceExisting()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "Collision_Replace.txt";
            IFile file1 = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
            await file1.WriteAllTextAsync("Hello, World");

            //  Act
            IFile file2 = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            //  Assert
            Assert.AreEqual(file2.Name, fileName);
            string file2Contents = await file2.ReadAllTextAsync();
            Assert.AreEqual(string.Empty, file2Contents);

            //  Cleanup
            await file2.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFileNameCollision_FailIfExists()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "Collision_Fail.txt";
            IFile file1 = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Act & Assert
            await ExceptionAssert.ThrowsAsync<IOException>(async () =>
                {
                    await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
                });

            //  Cleanup
            await file1.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFileNameCollision_OpenIfExists()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "Collision_OpenIfExists.txt";
            IFile file1 = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
            string contents = "Hello, World!";
            await file1.WriteAllTextAsync(contents);

            //  Act
            IFile file2 = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

            //  Assert
            Assert.AreEqual(file2.Name, fileName);
            string file2Contents = await file2.ReadAllTextAsync();
            Assert.AreEqual(contents, file2Contents);

            //  Cleanup
            await file2.DeleteAsync();
        }

		[TestMethod]
		public async Task WriteAndReadFile()
		{
			//	Arrange
			IFolder folder = Storage.AppLocalStorage;
			IFile file = await folder.CreateFileAsync("readWriteFile.txt", CreationCollisionOption.FailIfExists);
			string contents = "And so we beat on, boats against the current, born back ceaselessly into the past.";

			//	Act
			await file.WriteAllTextAsync(contents);
			string readContents = await file.ReadAllTextAsync();

			//	Assert
			Assert.AreEqual(contents, readContents);

			//	Cleanup
			await file.DeleteAsync();
		}

		[TestMethod]
		public async Task DeleteFile()
		{
			//	Arrange
			IFolder folder = Storage.AppLocalStorage;
			string fileName = "fileToDelete.txt";
			IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

			//	Act
			await file.DeleteAsync();

			//	Assert
			var files = await folder.GetFilesAsync();
			Assert.IsFalse(files.Any(f => f.Name == fileName));
		}

		[TestMethod]
		public async Task OpenDeletedFile()
		{
			//	Arrange
			IFolder folder = Storage.AppLocalStorage;
			string fileName = "fileToDeleteAndOpen.txt";
			IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
			await file.DeleteAsync();

			//	Act & Assert
			await ExceptionAssert.ThrowsAsync<IOException>(async () => { await file.OpenAsync(FileAccess.ReadAndWrite); });
		}

		[TestMethod]
		public async Task DeleteFileTwice()
		{
			//	Arrange
			IFolder folder = Storage.AppLocalStorage;
			string fileName = "fileToDeleteTwice.txt";
			IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
			await file.DeleteAsync();

			//	Act & Assert
			await ExceptionAssert.ThrowsAsync<IOException>(async () => { await file.DeleteAsync(); });
		}

        [TestMethod]
        public async Task OpenFileForRead()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "fileToOpenForRead.txt";
            IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Act
            using (Stream stream = await file.OpenAsync(FileAccess.Read))
            {

                //  Assert
                Assert.IsFalse(stream.CanWrite);
                Assert.IsTrue(stream.CanRead);
                Assert.IsTrue(stream.CanSeek);
            }

            //  Cleanup
            await file.DeleteAsync();
        }

        [TestMethod]
        public async Task OpenFileForReadAndWrite()
        {
            //  Arrange
            IFolder folder = Storage.AppLocalStorage;
            string fileName = "fileToOpenForReadAndWrite";
            IFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Act
            using (Stream stream = await file.OpenAsync(FileAccess.ReadAndWrite))
            {

                //  Assert
                Assert.IsTrue(stream.CanWrite);
                Assert.IsTrue(stream.CanRead);
                Assert.IsTrue(stream.CanSeek);                
            }

            //  Cleanup
            await file.DeleteAsync();
        }
    }
}
