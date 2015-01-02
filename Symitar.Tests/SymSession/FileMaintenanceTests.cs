using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class FileMaintenanceTests
    {
        [Test]
        public void UnitOfWork_Scenario_ExpectedBehavior()
        {
            var mockSocket = Substitute.For<ISymSocket>();
            mockSocket.ReadCommand().Returns(new SymCommand("FileList", new Dictionary<string, string> {{"Done", ""}}));

            var session = new SymSession(mockSocket, 10);

            int result = session.GetFileMaintenanceSequence("Report Title");
            result.Should().Be(-1);
        }
    }
}