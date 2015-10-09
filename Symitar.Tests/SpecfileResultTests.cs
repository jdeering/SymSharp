using System;
using FluentAssertions;
using NSubstitute.Core;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class SpecfileResultTests
    {
        [Test]
        public void SpecfileResult_NoErrors_PropertiesReturnMatchingValues()
        {
            var file = new File("symitar", "10", "FileName", FileType.RepGen, DateTime.Now, 100);
            var result = new SpecfileResult(file, "", "", 0, 0);

            result.Specfile.Should().Be(file);
            result.FileWithError.Should().Be("");
            result.ErrorMessage.Should().Be("");
            result.Line.Should().Be(0);
            result.Column.Should().Be(0);
        }

        [Test]
        public void SpecfileResult_WithErrors_PropertiesReturnMatchingValues()
        {
            var line = 100;
            var column = 1;
            var error = "Invalid character";
            var fileName = "FileName";
            var file = new File("symitar", "10", fileName, FileType.RepGen, DateTime.Now, 100);

            var result = new SpecfileResult(file, fileName, error, line, column);

            result.Specfile.Should().Be(file);
            result.FileWithError.Should().Be(fileName);
            result.ErrorMessage.Should().Be(error);
            result.Line.Should().Be(line);
            result.Column.Should().Be(column);
        }
        [Test]
        public void SpecfileResult_NoErrors_IsNotInvalid()
        {
            var file = new File("symitar", "10", "FileName", FileType.RepGen, DateTime.Now, 100);
            var result = new SpecfileResult(file, "", "", 0, 0);

            result.InvalidInstall.Should().BeFalse();
        }

        [Test]
        public void SpecfileResult_WithErrors_IsInvalidInstall()
        {
            var file = new File("symitar", "10", "FileName", FileType.RepGen, DateTime.Now, 100);

            var result = new SpecfileResult(file, "FileName", "Invalid character", 100, 1);

            result.InvalidInstall.Should().BeTrue();
        }

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