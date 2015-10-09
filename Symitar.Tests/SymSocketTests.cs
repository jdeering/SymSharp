using System;
using System.Linq;
using System.Text;
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

            socket.Error.Should().BeNullOrEmpty();
        }

        [Test]
        public void Connect_SuccessfulConnection_ReturnsTrue()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);

            socket.Connect("symitar", 23).Should().BeTrue();
        }

        [Test]
        public void ConnectNoArgs_NoServerSet_ThrowsArgumentNull()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            socket.Port = 23;

            Assert.Throws<ArgumentNullException>(() => socket.Connect());
        }

        [Test]
        public void ConnectNoArgs_ZeroPort_ThrowsArgumentOutOfRange()
        {
            var socket = new SymSocket();
            socket.Server = "symitar";

            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect());
        }

        [Test]
        public void ConnectNoArgs_AfterSettingServerAndPort_ReturnsTrue()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();

            var socket = new SymSocket(tcpAdapterMock);
            socket.Server = "symitar";
            socket.Port = 23;

            socket.Connect().Should().BeTrue();
        }

        [Test]
        public void Connect_ZeroPort_ThrowsArgumentOutOfRange()
        {
            var socket = new SymSocket();
            Assert.Throws<ArgumentOutOfRangeException>(() => socket.Connect("symitar", 0));
        }

        [Test]
        public void Connect_CallsUnderlyingClient()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            var socket = new SymSocket(tcpAdapterMock);

            socket.Connect("symitar", 23);

            tcpAdapterMock.Received().Connect("symitar", 23);
        }

        [Test]
        public void ConnectNoArgs_CallsUnderlyingClient()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            var socket = new SymSocket(tcpAdapterMock);
            socket.Server = "symitar";
            socket.Port = 23;

            socket.Connect();

            tcpAdapterMock.Received().Connect("symitar", 23);
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

        [Test]
        public void Write_String_CallsTcpAdapterWriteWithBytes()
        {
            var s = "data";
            var b = Encoding.ASCII.GetBytes(s);

            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            new SymSocket(tcpAdapterMock).Write(s);

            tcpAdapterMock.Received().Write(Arg.Is<Byte[]>(x => x.SequenceEqual(b)));
        }

        [Test]
        public void Write_ByteArray_CallsTcpAdapterWrite()
        {
            var b = Encoding.ASCII.GetBytes("data");

            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            new SymSocket(tcpAdapterMock).Write(b);

            tcpAdapterMock.Received().Write(b);
        }

        [Test]
        public void Write_SymCommand_CallsTcpAdapterWrite()
        {
            var cmd = Substitute.For<ISymCommand>();
            cmd.ToString().Returns("data");
            var b = Encoding.ASCII.GetBytes("data");

            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            new SymSocket(tcpAdapterMock).Write(cmd);

            tcpAdapterMock.Received().Write(Arg.Is<Byte[]>(x => x.SequenceEqual(b)));
        }

        [Test]
        public void WakeUp_WritesToTcpAdapter()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            new SymSocket(tcpAdapterMock).WakeUp();

            tcpAdapterMock.Received().Write(Arg.Any<Byte[]>());
        }

        [Test]
        public void WakeUp_TcpAdapaterException_DoesNotThrowExceptionLater()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock
                .WhenForAnyArgs(x => x.Write(new byte[1]))
                .Do(x => { throw new Exception(); });

            Assert.DoesNotThrow(() =>
            {
                new SymSocket(tcpAdapterMock).WakeUp(); 
            });
        }

        [Test]
        public void Read_NoData_ReturnsEmptyString()
        {
            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock.Read().Returns(new byte[0]);

            var socket = new SymSocket(tcpAdapterMock);

            socket.Read().Should().Be("");
        }

        [Test]
        public void Read_SingleChunk_ReturnsCorrectString()
        {
            var chunk = Encoding.ASCII.GetBytes("data");

            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock.Read().Returns(x => chunk, x => new byte[0]);

            var socket = new SymSocket(tcpAdapterMock);

            socket.Read().Should().Be("data");
        }

        [Test]
        public void Read_MultipleChunks_ConcatenatesCorrectly()
        {
            var chunk1 = Encoding.ASCII.GetBytes("doubl");
            var chunk2 = Encoding.ASCII.GetBytes("e data");

            var tcpAdapterMock = Substitute.For<ITcpAdapter>();
            tcpAdapterMock.Read().Returns(x => chunk1, x => chunk2, x => new byte[0]);

            var socket = new SymSocket(tcpAdapterMock);

            socket.Read().Should().Be("double data");
        }
    }
}