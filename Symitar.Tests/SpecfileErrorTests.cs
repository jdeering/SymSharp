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
            SpecfileError error = SpecfileError.None();
            error.FailedCheck.Should().BeFalse();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeOver0_DoesntFailCheck()
        {
            SpecfileError error = SpecfileError.None(100);
            error.FailedCheck.Should().BeFalse();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSize_HasCorrectInstallSize()
        {
            SpecfileError error = SpecfileError.None(100);
            error.InstallSize.Should().Be(100);
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeEqual0_FailsCheck()
        {
            SpecfileError error = SpecfileError.None(0);
            error.FailedCheck.Should().BeTrue();
        }

        [Test]
        public void SpecfileError_NoneWithInstallSizeUnder0_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    SpecfileError error = SpecfileError.None(-1);
                }
            );
        }
    }
}
