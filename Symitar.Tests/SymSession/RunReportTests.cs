using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Rhino.Mocks;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class RunReportTests
    {
        [Test]
        public void FileRun_LetterFile_ThrowsException()
        {
            var file = new File("symitar", "10", "RandomFile", FileType.Letter, DateTime.Now, 10);

            var session = new SymSession(10);
            Assert.Throws<InvalidOperationException>(() => session.FileRun(file, null, null, -1));
        }

        [Test]
        public void IsFileRunning_DoneImmediate_ReturnsFalse()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeFalse();
        }

        [Test]
        public void IsFileRunning_NegativeSequence_ThrowsOutOfRange()
        {
            var session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(() => session.IsFileRunning(-1));
        }

        [Test]
        public void IsFileRunning_QueueEntryWithMatchingSeq_ReturnsTrue()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Misc",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Action", "QueueEntry"},
                                                     {"Seq", "1"}
                                                 })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeTrue();
        }

        [Test]
        public void IsFileRunning_QueueEntryWithoutMatchingSeq_ReturnsTrue()
        {
            var mockSocket = MockRepository.GenerateMock<ISymSocket>();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Misc",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Action", "QueueEntry"},
                                                     {"Seq", "11"}
                                                 })).Repeat.Once();
            mockSocket.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeFalse();
        }

        [Test]
        public void IsFileRunning_ZeroSequence_ThrowsOutOfRange()
        {
            var session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(() => session.IsFileRunning(0));
        }
    }
}