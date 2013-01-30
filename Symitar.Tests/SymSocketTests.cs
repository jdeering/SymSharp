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
    public class SymSocketTests
    {
        [Test]
        public void Connect_NullServer_ThrowsArgumentNull()
        {
            SymSocket socket = new SymSocket();
            Assert.Throws<ArgumentNullException>(() => socket.Connect(null, 1));
        }

        [Test]
        public void Connect_EmptyServer_ThrowsArgumentNull()
        {
            SymSocket socket = new SymSocket();
            Assert.Throws<ArgumentNullException>(() => socket.Connect("", 1));
        }

        [Test]
        public void Connect_NegativePort_ThrowsArgumentOutOfRange()
        {
            SymSocket socket = new SymSocket();
            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect("symitar", -1));
        }

        [Test]
        public void Connect_ZeroPort_ThrowsArgumentOutOfRange()
        {
            SymSocket socket = new SymSocket();
            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect("symitar", 0));
        }

        [Test]
        public void Connect_AlreadyConnected_ThrowsInvalidOperation()
        {
            var tcpMock = MockRepository.GenerateMock<ITcpAdapter>();
            tcpMock.Stub(x => x.Connected).Return(true);
            
            SymSocket socket = new SymSocket(tcpMock);
            Assert.Throws<InvalidOperationException>(() => socket.Connect("symitar", 23));
        }

        [Test]
        public void Connect_ClientConnectException_ReturnsFalse()
        {
            var tcpMock = MockRepository.GenerateMock<ITcpAdapter>();
            tcpMock.Stub(x => x.Connect("symitar", 23)).Throw(new InvalidOperationException());

            SymSocket socket = new SymSocket(tcpMock);
            var result = socket.Connect("symitar", 23);
            result.Should().BeFalse();
        }

        [Test]
        public void Connect_ClientConnectException_HasErrorMessage()
        {
            var tcpMock = MockRepository.GenerateMock<ITcpAdapter>();
            tcpMock.Stub(x => x.Connect("symitar", 23)).Throw(new InvalidOperationException());

            SymSocket socket = new SymSocket(tcpMock);
            socket.Connect("symitar", 23);
            socket.Error.Should().Contain("Unable to Connect to Server");
        }

        [Test]
        public void Connect_SuccessfulConnection_NoError()
        {
            var tcpMock = MockRepository.GenerateMock<ITcpAdapter>();

            SymSocket socket = new SymSocket(tcpMock);
            socket.Connect("symitar", 23);
            socket.Error.Should().BeBlank();
        }

        [Test]
        public void Connect_SuccessfulConnection_ReturnsTrue()
        {
            var tcpMock = MockRepository.GenerateMock<ITcpAdapter>();

            SymSocket socket = new SymSocket(tcpMock);
            bool result = socket.Connect("symitar", 23);
            result.Should().BeTrue();
        }

        [Test]
        public void ReadUntilMultiMatch_EmptyMatcherSet_ThrowsArugmentException()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    {
                        var socket = new SymSocket();

                        socket.ReadUntil(new List<byte[]>(), 1);
                    }
                );
        }

        [Test]
        public void ReadUntilMultiMatch_ZeroTimeout_ThrowsArugmentException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    var socket = new SymSocket();

                    socket.ReadUntil(new List<byte[]>(){ new byte[1] }, 0);
                }
                );
        }
    }
}
