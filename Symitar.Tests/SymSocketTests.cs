using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class SymSocketTests
    {
        [Test]
        public void Connect_AlreadyConnected_ThrowsInvalidOperation()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock.Connected.Returns(true);

            var socket = new SymSocket(tcpAdapterMock);
            Assert.Throws<InvalidOperationException>(() => socket.Connect("symitar", 23));
        }

        [Test]
        public void Connect_ClientConnectException_HasErrorMessage()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock
                .When(x => x.Connect("symitar", 23))
                .Do(x => { throw new InvalidOperationException(); });

            var socket = new SymSocket(tcpAdapterMock);
            socket.Connect("symitar", 23);
            socket.Error.Should().Contain("Unable to Connect to Server");
        }

        [Test]
        public void Connect_ClientConnectException_ReturnsFalse()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock
                .When(x => x.Connect("symitar", 23))
                .Do(x => { throw new InvalidOperationException(); });

            var socket = new SymSocket(tcpAdapterMock);
            bool result = socket.Connect("symitar", 23);
            result.Should().BeFalse();
        }

        [Test]
        public void Connect_EmptyServer_ThrowsArgumentNull()
        {
            var socket = new SymSocket();
            Assert.Throws<ArgumentNullException>(() => socket.Connect("", 1));
        }

        [Test]
        public void Connect_NegativePort_ThrowsArgumentOutOfRange()
        {
            var socket = new SymSocket();
            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect("symitar", -1));
        }

        [Test]
        public void Connect_NullServer_ThrowsArgumentNull()
        {
            var socket = new SymSocket();
            Assert.Throws<ArgumentNullException>(() => socket.Connect(null, 1));
        }

        [Test]
        public void Connect_SuccessfulConnectionWithIpAddress_CallsHostNameConnectOnAdapter()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            socket.Connect("symitar", 23);
            tcpAdapterMock.Received().Connect("symitar", 23);
        }

        [Test]
        public void Connect_SuccessfulConnectionWithIpAddress_ReturnsTrue()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            bool result = socket.Connect("127.0.0.1", 23);
            result.Should().BeTrue();
        }

        [Test]
        public void Connect_SuccessfulConnection_HasBlankError()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            socket.Connect("symitar", 23);
            socket.Error.Should().BeBlank();
        }

        [Test]
        public void Connect_SuccessfulConnection_ReturnsTrue()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            bool result = socket.Connect("symitar", 23);
            result.Should().BeTrue();
        }

        [Test]
        public void Connect_ZeroPort_ThrowsArgumentOutOfRange()
        {
            var socket = new SymSocket();
            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect("symitar", 0));
        }

        [Test]
        public void Disconnect_ClosesClient()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            socket.Disconnect();
            tcpAdapterMock.Received().Close();
        }

        [Test]
        public void ReadUntilMultiMatch_EmptyMatcherSet_ThrowsArugmentException()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    {
                        var socket = new SymSocket();
                        socket.WaitFor();
                    }
                );
        }
    }
}