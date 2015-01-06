using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class FileHandlingTests
    {
        [Test]
        public void FileCheck_ActionFileInfo_FailsCheck()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "FileInfo"},{"Line", "22"},{"Col", "1"}}),
                        new SymCommand("Check", new Dictionary<string, string> {{"Action", "DisplayEdit"}})
                        );

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            SpecfileResult result = session.FileCheck(file);

            result.PassedCheck.Should().BeFalse();
        }

        [Test]
        public void FileCheck_ActionNoError_PassesCheck()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Action", "NoError"}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            SpecfileResult result = session.FileCheck(file);

            result.PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileCheck_CommandHasError_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Error", ""}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_CommandHasWarning_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Warning", ""}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_LetterFile_ThrowsException()
        {
            var mockSocket = Substitute.For<ISymSocket>();

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.Letter, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileCheck(file));
        }

        [Test]
        public void FileCheck_UnknownCommands_ThrowsException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Action", "Random"}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileCheck(file), "An unknown error occurred.");
        }

        [Test]
        public void FileDelete_CompletesSuccessfully_NoExceptions()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Rename", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            Assert.DoesNotThrow(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileDelete_NoStatusOrDone_ThrowsException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Unknown"));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileDelete_StatusNoSuchFile_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Rename",
                                             new Dictionary<string, string> {{"Status", "No such file or directory"}}));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileDelete("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileDelete_StatusUnknown_ThrowsFileNotFoundException()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Rename", new Dictionary<string, string> {{"Status", ""}}));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileDelete("RandomFile", FileType.RepGen), "Filename Too Long");
        }

        [Test]
        public void FileExistsByFile_FilesReturned_IsTrue()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("FileList",
                        new Dictionary<string, string>
                            {
                                {"Name", "RandomFile"},
                                {"Date", "01012013"},
                                {"Time", "1153"},
                                {"Size", "1123"},
                            }),
                    new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}})
                );

            var session = new SymSession(mockSocket, 10);

            bool result = session.FileExists(file);
            result.Should().BeTrue();
        }

        [Test]
        public void FileExistsByFile_NoFilesReturned_IsFalse()
        {
            var file = new File("symitar", "000", "RandomFile", FileType.RepGen, DateTime.Now, 100);

            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            bool result = session.FileExists(file);
            result.Should().BeFalse();
        }

        [Test]
        public void FileExistsByName_FilesReturned_IsTrue()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("FileList", new Dictionary<string, string>
                        {
                            {"Name", "RandomFile"},
                            {"Date", "01012013"},
                            {"Time", "1153"},
                            {"Size", "1123"},
                        }),
                    new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}})
                );

            var session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeTrue();
        }

        [Test]
        public void FileExistsByName_NoFilesReturned_IsFalse()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeFalse();
        }

        [Test]
        public void FileGet_HasMultipleMatches_ReturnsFirstFile()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.Server.Returns("symitar");
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("FileList", new Dictionary<string, string>
                                                 {
                                                     {"Name", "RandomFile"},
                                                     {"Date", "01012013"},
                                                     {"Time", "1153"},
                                                     {"Size", "1123"},
                                                 }),
                    new SymCommand("FileList", new Dictionary<string, string>
                                                 {
                                                     {"Name", "RandomFile2"},
                                                     {"Date", "01012013"},
                                                     {"Time", "1153"},
                                                     {"Size", "1123"},
                                                 }),
                    new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}})
                );

            var expected = new File("symitar", "10", "RandomFile", FileType.RepGen, "01012013", "1153", 1123);

            var session = new SymSession(mockSocket, 10);

            File actual = session.FileGet("RandomFile+", FileType.RepGen);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        [Test]
        public void FileGet_HasOneMatch_ReturnsFile()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.Server.Returns("symitar");
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("FileList",
                                            new Dictionary<string, string>
                                                {
                                                    {"Name", "RandomFile"},
                                                    {"Date", "01012013"},
                                                    {"Time", "1153"},
                                                    {"Size", "1123"},
                                                }),
                    new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}})
                );

            var expected = new File("symitar", "10", "RandomFile", FileType.RepGen, "01012013", "1153", 1123);

            var session = new SymSession(mockSocket, 10);

            File actual = session.FileGet("RandomFile", FileType.RepGen);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        [Test]
        public void FileGet_NoMatches_ThrowsFileNotFound()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileGet("RandomFile", FileType.RepGen));
        }

        [Test]
        public void FileInstall_ActionFileInfo_FailsCheck()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "Init"}}),
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "FileInfo"},{"Line", "22"},{"Col", "1"}}),
                    new SymCommand("Check", new Dictionary<string, string> {{"Action", "DisplayEdit"}})
                );

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            session.FileInstall(file).PassedCheck.Should().BeFalse();
        }

        [Test]
        public void FileInstall_ActionNoError_PassesCheck()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "DisplayEdit" } })
                );

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            session.FileInstall(file).PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileInstall_CommandHasError_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Error", ""}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_CommandHasWarning_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Check", new Dictionary<string, string> {{"Warning", ""}}));

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_LetterFile_ThrowsException()
        {
            var mockSocket = Substitute.For<ISymSocket>();

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.Letter, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileInstall(file));
        }

        [Test]
        public void FileInstall_SpecfileData_PassesCheck()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("Check", new Dictionary<string, string> { { "Action", "Init" } }),
                    new SymCommand("SpecfileData", new Dictionary<string, string> { { "Size", "110" } })
                );

            var file = new File("symitar", "10", "FILE.TO.CHECK", FileType.RepGen, DateTime.Now, 110);
            var session = new SymSession(mockSocket, 10);
            session.FileInstall(file).PassedCheck.Should().BeTrue();
        }

        [Test]
        public void FileList_HasMatches_CountIsCorrect()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("FileList", new Dictionary<string, string>
                                                 {
                                                     {"Name", "RandomFile"},
                                                     {"Date", "01012013"},
                                                     {"Time", "1153"},
                                                     {"Size", "1123"},
                                                 }),
                    new SymCommand("FileList", new Dictionary<string, string>
                                                 {
                                                     {"Name", "RandomFile2"},
                                                     {"Date", "01012013"},
                                                     {"Time", "1153"},
                                                     {"Size", "1123"},
                                                 }),
                    new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } })
                );

            var session = new SymSession(mockSocket, 10);

            session.FileList("RandomFile+", FileType.RepGen).Count.Should().Be(2);
        }

        [Test]
        public void FileList_NoMatches_CountIsZero()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            session.FileList("--", FileType.RepGen).Count.Should().Be(0);
        }

        [Test]
        public void FileRead_BlankReport_ReturnsEmptyString()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Read",
                                             new Dictionary<string, string> {{"Status", "Cannot view a blank report"}}));

            var session = new SymSession(mockSocket, 10);
            string result = session.FileRead("002356", FileType.Report);
            result.Should().BeNullOrEmpty();
        }

        [Test]
        public void FileRead_LongFileName_ThrowException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Read", new Dictionary<string, string> {{"Status", ""}}));

            var session = new SymSession(mockSocket, 10);
            Assert.Throws<Exception>(() => session.FileRead("ReallyReallReallyReallyLongFileName", FileType.Report), "Filename Too Long");
        }

        [Test]
        public void FileRead_NoSuchFile_ThrowFileNotFound()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Read", new Dictionary<string, string> {{"Status", "No such file or directory"}}));

            var session = new SymSession(mockSocket, 10);
            Assert.Throws<FileNotFoundException>(() => session.FileRead("002356", FileType.Report));
        }

        [Test]
        public void FileRename_CompletesSuccessfully_NoExceptions()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                      .Returns(new SymCommand("Rename", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            Assert.DoesNotThrow(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileRename_NoStatusOrDone_ThrowsException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Unknown"));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileRename_StatusNoSuchFile_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Rename", new Dictionary<string, string> {{"Status", "No such file or directory"}}));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<FileNotFoundException>(() => session.FileRename("OldName", "NewName", FileType.RepGen));
        }

        [Test]
        public void FileRename_StatusUnknown_ThrowsFileNotFoundException()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("Rename", new Dictionary<string, string> {{"Status", ""}}));

            var session = new SymSession(mockSocket, 10);

            Assert.Throws<Exception>(() => session.FileRename("OldName", "NewName", FileType.RepGen),
                                     "Filename Too Long");
        }
    }
}