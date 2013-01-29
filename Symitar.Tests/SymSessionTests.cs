using System;
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
        public void Connect_NullServer_ThrowsArgumentException()
        {
            BeforeConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Connect(null, 1));
        }

        [Test]
        public void Connect_BlankServer_ThrowsArgumentException()
        {
            BeforeConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Connect("", 1));
        }

        [Test]
        public void Connect_ZeroPort_ThrowsArgumentException()
        {
            SymSession session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => session.Connect("symitar", 0));
        }

        [Test]
        public void Connect_NegativePort_ThrowsArgumentException()
        {
            SymSession session = new SymSession();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => session.Connect("symitar", -1));
        }

        [Test]
        public void Login_NotConnected_ReturnsFalse()
        {
            BeforeConnect();
            bool result = _session.Login("bob", "dole", 0, "bobdole");
            result.Should().BeFalse();
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
        public void Login_BlankUsername_ThrowsArgumentException()
        {
            AfterConnect();
            Assert.Throws<ArgumentNullException>(
                () => _session.Login("", "dole", 0, "bobdole"));
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
        public void Login_NegativeSymDir_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", -1, "bobdole"));
        }

        [Test]
        public void Login_1000SymDir_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", 1000, "bobdole"));
        }

        [Test]
        public void Login_NegativeStage_ThrowsArgumentOutOfRangeException()
        {
            AfterConnect();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _session.Login("bob", "dole", 10, "bobdole", -1));
        }
    }
}
