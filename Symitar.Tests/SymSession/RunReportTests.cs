using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
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
            var mockSocket = Substitute.For<ISymSocket>();
            var session = new SymSession(mockSocket, 10);

            Assert.Throws<InvalidOperationException>(() => session.FileRun(file, null, null, -1));
        }

        [Test]
        public void IsFileRunning_DoneImmediate_ReturnsFalse()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeFalse();
        }

        [Test]
        public void IsFileRunning_NegativeSequence_ThrowsOutOfRange()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            var session = new SymSession(mockSocket);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.IsFileRunning(-1));
        }

        [Test]
        public void IsFileRunning_QueueEntryWithMatchingSeq_ReturnsTrue()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("Misc", new Dictionary<string, string>{{"Action", "QueueEntry"},{"Seq", "1"}}),
                    new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}})
                );

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeTrue();
        }

        [Test]
        public void IsFileRunning_QueueEntryWithoutMatchingSeq_ReturnsTrue()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand()
                .Returns(
                    new SymCommand("Misc",new Dictionary<string, string>{{"Action", "QueueEntry"},{"Seq", "11"}}),
                    new SymCommand("Misc", new Dictionary<string, string> {{"Done", ""}})
                );

            var session = new SymSession(mockSocket);
            session.IsFileRunning(1).Should().BeFalse();
        }

        [Test]
        public void IsFileRunning_ZeroSequence_ThrowsOutOfRange()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            var session = new SymSession(mockSocket);

            Assert.Throws<ArgumentOutOfRangeException>(() => session.IsFileRunning(0));
        }
    }
}