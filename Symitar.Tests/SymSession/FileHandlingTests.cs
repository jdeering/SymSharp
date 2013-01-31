using System;
using System.Collections.Generic;
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
        public void FileExists_NoFilesReturned_IsFalse()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeFalse();
        }

        [Test]
        public void FileExists_FilesReturned_IsTrue()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList", 
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            bool result = session.FileExists("RandomFile", FileType.RepGen);
            result.Should().BeTrue();
        }

        [Test]
        public void FileList_NoMatches_CountIsZero()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            var result = session.FileList("--", FileType.RepGen);
            result.Count.Should().Be(0);
        }

        [Test]
        public void FileList_HasMatches_CountIsCorrect()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList",
                                new Dictionary<string, string>
                                    {
                                        { "Name", "RandomFile2" },
                                        { "Date", "01012013" },
                                        { "Time", "1153" },
                                        { "Size", "1123" },
                                    })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand(2000))
                      .Return(new SymCommand("FileList", new Dictionary<string, string> { { "Done", "" } }));

            SymSession session = new SymSession(mockSocket, 10);

            var result = session.FileList("RandomFile+", FileType.RepGen);
            result.Count.Should().Be(2);
        }
    }
}
