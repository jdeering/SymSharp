using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using FluentAssertions;
using NUnit.Framework;
using Rhino.Mocks;
using Symitar.Interfaces;

namespace Symitar.Tests
{
    [TestFixture]
    public class SymSessionTests
    {
        private SymSession _session;

        private void BeforeConnect()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connect()).Return(true);
            _session = new SymSession(socketMock);
        }

        private void AfterConnect()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connect()).Return(true);
            socketMock.Stub(x => x.Connected).Return(true);
            _session = new SymSession(socketMock);
            _session.Connect("symitar", 23);
        }

        [Test]
        public void Connect_BlankServer_ThrowsArgumentException()
        {
            BeforeConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Connect("", 1));
        }

        [Test]
        public void Connect_NegativePort_ThrowsArgumentException()
        {
            var session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => session.Connect("symitar", -1));
        }

        [Test]
        public void Connect_NullServer_ThrowsArgumentException()
        {
            BeforeConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Connect(null, 1));
        }

        [Test]
        public void Connect_ZeroPort_ThrowsArgumentException()
        {
            var session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => session.Connect("symitar", 0));
        }

        [Test]
        public void Constructor_WithSocketAndSymDir_HasCorrectSymDir()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connect()).Return(true);
            var session = new SymSession(socketMock, 10);
            session.SymDirectory.Should().Be(10);
        }

        [Test]
        public void Constructor_WithSymDir_HasCorrectSymDir()
        {
            var session = new SymSession(10);
            session.SymDirectory.Should().Be(10);
        }

        [Test]
        public void Disconnect_CallsDisconnectOnSocket()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            var session = new SymSession(socketMock);
            session.Disconnect();
            socketMock.AssertWasCalled(x => x.Disconnect());
        }

        [Test]
        public void Login_1000SymDir_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", 1000, "bobdole"));
        }

        [Test]
        public void Login_BlankPassword_ThrowsArgumentException()
        {
            AfterConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Login("bob", "", 0, "bobdole"));
        }

        [Test]
        public void Login_BlankUserId_ThrowsArgumentException()
        {
            AfterConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Login("bob", "dole", 0, ""));
        }

        [Test]
        public void Login_BlankUsername_ThrowsArgumentException()
        {
            AfterConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Login("", "dole", 0, "bobdole"));
        }

        [Test]
        public void Login_InputHasHelpCode_CallsHostSyncWrite()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Request")).Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Input", new Dictionary<string, string> {{"HelpCode", "10025"}}));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            socketMock.AssertWasCalled(x => x.Write("$WinHostSync$\r"));
        }

        [Test]
        public void Login_InvalidAixLoginPassword_HasErrorMessage()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return("invalid login");

            var session = new SymSession(socketMock, 10);
            session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Invalid AIX Login");
        }

        [Test]
        public void Login_InvalidAixLoginPassword_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return("invalid login");

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_InvalidAixLoginUsername_HasErrorMessage()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("invalid login");

            var session = new SymSession(socketMock, 10);
            session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Invalid AIX Login");
        }

        [Test]
        public void Login_InvalidAixLoginUsername_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c"));
            socketMock.Stub(x => x.Read()).Return("invalid login");

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_InvalidSymLogin_HasErrorMessage()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonInvalidUser"));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Invalid Sym User");
        }

        [Test]
        public void Login_InvalidSymLogin_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonInvalidUser"));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_LocksDuringLogin_HasErrorMessage()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonError",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Text", "Too Many Invalid Password Attempts"}
                                                 }));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Too Many Invalid Password Attempts");
        }

        [Test]
        public void Login_LocksDuringLogin_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonError",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Text", "Too Many Invalid Password Attempts"}
                                                 }));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_NegativeStage_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", 10, "bobdole", -1));
        }

        [Test]
        public void Login_NegativeSymDir_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", -1, "bobdole"));
        }

        [Test]
        public void Login_NotConnected_HasError()
        {
            BeforeConnect();
            bool result = _session.Login("bob", "dole", 0, "bobdole");

            _session.Error.Should().Be("Socket not connected");
        }

        [Test]
        public void Login_NotConnected_NotLoggedIn()
        {
            BeforeConnect();
            bool result = _session.Login("bob", "dole", 0, "bobdole");

            _session.LoggedIn.Should().BeFalse();
        }

        [Test]
        public void Login_NotConnected_ReturnsFalse()
        {
            BeforeConnect();
            bool result = _session.Login("bob", "dole", 0, "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_SocketException_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c"))
                      .Throw(new InvalidOperationException());

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_SocketKeepAliveFail_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.KeepAliveStart())
                      .Throw(new InvalidOperationException());

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_SocketWriteFail_HasError()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.Write(new byte[] {0xFF, 0xFB, 0x18}))
                      .Throw(new NetworkInformationException());

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Telnet communication failed");
        }

        [Test]
        public void Login_SocketWriteFail_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.Write(new byte[] {0xFF, 0xFB, 0x18}))
                      .Throw(new NetworkInformationException());

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }

        [Test]
        public void Login_Successful_StartsKeepAliveOnSocket()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("Input", new Dictionary<string, string> {{"HelpCode", "10025"}}));

            var session = new SymSession(socketMock, 10);
            session.Login("bob", "dole", "bobdole");
            socketMock.AssertWasCalled(x => x.KeepAliveStart());
        }

        [Test]
        public void Login_SymLocked_HasErrorMessage()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonError",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Text", "Too Many Invalid Password Attempts"}
                                                 }));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            session.Error.Should().Contain("Too Many Invalid Password Attempts");
        }

        [Test]
        public void Login_SymLocked_ReturnsFalse()
        {
            var socketMock = MockRepository.GenerateStub<ISymSocket>();
            socketMock.Stub(x => x.Connected).Return(true);
            socketMock.Stub(x => x.WaitFor("Password:", "[c")).Return(0);
            socketMock.Stub(x => x.Read()).Return("Password:").Repeat.Once();
            socketMock.Stub(x => x.WaitFor(":")).Return(0);
            socketMock.Stub(x => x.Read()).Return(":").Repeat.Once();
            socketMock.Stub(x => x.ReadCommand())
                      .Return(new SymCommand("SymLogonError",
                                             new Dictionary<string, string>
                                                 {
                                                     {"Text", "Too Many Invalid Password Attempts"}
                                                 }));

            var session = new SymSession(socketMock, 10);
            bool result = session.Login("bob", "dole", "bobdole");
            result.Should().BeFalse();
        }
    }
}