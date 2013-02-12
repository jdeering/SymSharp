using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Rhino.Mocks;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class FileHandlingTests
    {
        [Test]
        public void FileExistsByName_NoFilesReturned_IsFalse()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeFalse();
        }

        [Test]
        public void FileExistsByName_FilesReturned_IsTrue()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", 
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeTrue();
        }

        [Test]
        public void FileList_NoMatches_CountIsZero()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            var result = session.FileList("--", FileType.RepGen);
            result.Count.Should().Be(0);
        }

        [Test]
        public void FileList_HasMatches_CountIsCorrect()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile2" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            var result = session.FileList("RandomFile+", FileType.RepGen);
            result.Count.Should().Be(2);
        }

        [Test]
        public void FileExistsByFile_NoFilesReturned_IsFalse()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists(file);
            result.Should().BeFalse();
        }

        [Test]
        public void FileExistsByFile_FilesReturned_IsTrue()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists(file);
            result.Should().BeTrue();
        }

        [Test]
        public void FileGet_NoMatches_ThrowsFileNotFound()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileGet("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileGet_HasOneMatch_ReturnsFile()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.Server).Return("symitar");
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            File expected = new File("symitar", "10", "RandomFile", FileType.RepGen, "01012013", "1153", 1123);

            SymSession session = new SymSession(mockSocket, 10);

            File actual = session.FileGet("RandomFile", FileType.RepGen);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        [Test]
        public void FileGet_HasMultipleMatches_ReturnsFirstFile()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.Server).Return("symitar");
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile2" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            File expected = new File("symitar", "10", "RandomFile", FileType.RepGen, "01012013", "1153", 1123);

            SymSession session = new SymSession(mockSocket, 10);

            File actual = session.FileGet("RandomFile+", FileType.RepGen);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        [Test]
        public void FileRename_NoStatusOrDone_ThrowsException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand()).Return(new SymCommand("Unknown"));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileRename_StatusNoSuchFile_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Status", "No such file or directory" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileRename_StatusUnknown_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Status", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileRename("OldName", "NewName", FileType.RepGen), "Filename Too Long");
        }

        [Test]
        public void FileRename_CompletesSuccessfully_NoExceptions()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.DoesNotThrow(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileDelete_NoStatusOrDone_ThrowsException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand()).Return(new SymCommand("Unknown"));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileDelete_StatusNoSuchFile_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Status", "No such file or directory" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileDelete_StatusUnknown_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Status", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileDelete("RandomFile", FileType.RepGen), "Filename Too Long");
        }

        [Test]
        public void FileDelete_CompletesSuccessfully_NoExceptions()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Rename", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            Assert.DoesNotThrow(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileRead_BlankReport_ReturnsEmptyString()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Read", new Dictionary<string, string> { { "Status", "Cannot view a blank report" } }));

            SymSession session = new SymSession(mockSocket, 10);
            string result = session.FileRead("002356", FileType.Report);
            result.Should().BeBlank();
        }

        [Test]
        public void FileRead_NoSuchFile_ThrowFileNotFound()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Read", new Dictionary<string, string> { { "Status", "No such file or directory" } }));

            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileRead("002356", FileType.Report));
        }

        [Test]
        public void FileRead_LongFileName_ThrowException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Read", new Dictionary<string, string> { { "Status", "" } }));

            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileRead("ReallyLongFileName", FileType.Report), "Filename Too Long");
        }

        [Test]
        public void FileCheck_LetterFile_ThrowsException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.Letter, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_CommandHasWarning_ThrowsFileNotFoundException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Warning", "" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_CommandHasError_ThrowsFileNotFoundException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Error", "" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_ActionNoError_PassesCheck()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "NoError" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            var result = session.FileCheck(file);

            result.PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileCheck_ActionFileInfo_FailsCheck()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } })).Repeat.Times(4);
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", 
                          new Dictionary<string, string>
                              {
                                  { "Action", "FileInfo" },
                                  { "Line", "22" },
                                  { "Col", "1" }
                              })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "DisplayEdit" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            var result = session.FileCheck(file);

            result.PassedCheck.Should().BeFalse();
        }

        [Test]
        public void FileCheck_UnknownCommands_ThrowsException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "Random" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileCheck(file), "An unknown error occurred.");
        }

        [Test]
        public void FileInstall_LetterFile_ThrowsException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.Letter, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_CommandHasWarning_ThrowsFileNotFoundException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Warning", "" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_CommandHasError_ThrowsFileNotFoundException()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Error", "" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_ActionNoError_PassesCheck()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } })).Repeat.Times(4);
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "DisplayEdit" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            var result = session.FileInstall(file);

            result.PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileInstall_SpecfileData_PassesCheck()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check")).Repeat.Times(3);
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SpecfileData", new Dictionary<string, string> { { "Size", "110" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            var result = session.FileInstall(file);

            result.PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileInstall_ActionFileInfo_FailsCheck()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } })).Repeat.Times(4);
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check",
                          new Dictionary<string, string>
                              {
                                  { "Action", "FileInfo" },
                                  { "Line", "22" },
                                  { "Col", "1" }
                              })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Check", new Dictionary<string, string> { { "Action", "DisplayEdit" } }));

            File file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            SymSession session = new SymSession(mockSocket, 10);
            var result = session.FileInstall(file);

            result.PassedCheck.Should().BeFalse();
        }
    }
}
