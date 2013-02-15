using System;
using FluentAssertions;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class SpecfileResultTests
    {
        [Test]
        public void SpecfileError_SuccessWithInstallSizeEqual0_FailsCheck()
        {
            SpecfileResult result = SpecfileResult.Success(0);
            result.PassedCheck.Should().BeFalse();
        }

        [Test]
        public void SpecfileError_SuccessWithInstallSizeOver0_PassesCheck()
        {
            SpecfileResult result = SpecfileResult.Success(100);
            result.PassedCheck.Should().BeTrue();
        }

        [Test]
        public void SpecfileError_SuccessWithInstallSizeUnder0_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => { SpecfileResult result = SpecfileResult.Success(-1); }
                );
        }

        [Test]
        public void SpecfileError_SuccessWithInstallSize_HasCorrectInstallSize()
        {
            SpecfileResult result = SpecfileResult.Success(100);
            result.InstallSize.Should().Be(100);
        }

        [Test]
        public void SpecfileError_Success_PassesCheck()
        {
            SpecfileResult result = SpecfileResult.Success();
            result.PassedCheck.Should().BeTrue();
        }
    }
}