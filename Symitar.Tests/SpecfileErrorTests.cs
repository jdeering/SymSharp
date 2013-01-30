using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace Symitar.Tests
{
    [TestFixture]
    public class SpecfileErrorTests
    {
        [Test]
        public void SpecfileError_None_DoesntFailCheck()
        {
            SpecfileResult result = SpecfileResult.Success();
            result.FailedCheck.Should().BeFalse();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeOver0_DoesntFailCheck()
        {
            SpecfileResult result = SpecfileResult.Success(100);
            result.FailedCheck.Should().BeFalse();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSize_HasCorrectInstallSize()
        {
            SpecfileResult result = SpecfileResult.Success(100);
            result.InstallSize.Should().Be(100);
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeEqual0_FailsCheck()
        {
            SpecfileResult result = SpecfileResult.Success(0);
            result.FailedCheck.Should().BeTrue();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeUnder0_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    SpecfileResult result = SpecfileResult.Success(-1);
                }
            );
        }
    }
}
