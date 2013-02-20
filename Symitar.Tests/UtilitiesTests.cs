using System;
using FluentAssertions;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class UtilitiesTests
    {
        [Test]
        public void ContainingFolder_3DigitSymNum_ReturnsSuccessfully()
        {
            string expected = "/SYM/SYM000/LETTERSPECS";
            string actual = Utilities.ContainingFolder("0", FileType.Letter);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ContainingFolder_EmptySymNum_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => Utilities.ContainingFolder("", FileType.Letter)
                );
        }

        [Test]
        public void ContainingFolder_IntSymNum999_ReturnsSuccessfully()
        {
            string expected = "/SYM/SYM999/LETTERSPECS";
            string actual = Utilities.ContainingFolder(999, FileType.Letter);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ContainingFolder_IntSymNumOver999_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Utilities.ContainingFolder(1000, FileType.Letter)
                );
        }

        [Test]
        public void ContainingFolder_IntSymNum_Has3DigitNum()
        {
            string expected = "/SYM/SYM010/LETTERSPECS";
            string actual = Utilities.ContainingFolder(10, FileType.Letter);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ContainingFolder_LongSymNum_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Utilities.ContainingFolder("0000", FileType.Letter)
                );
        }

        [Test]
        public void ContainingFolder_NegativeIntSymNum_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Utilities.ContainingFolder(-1, FileType.Letter)
                );
        }

        [Test]
        public void ContainingFolder_NullSymNum_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => Utilities.ContainingFolder(null, FileType.Letter)
                );
        }

        [Test]
        public void ContainingFolder_ShortSymNum_Has3DigitNum()
        {
            string expected = "/SYM/SYM000/LETTERSPECS";
            string actual = Utilities.ContainingFolder("0", FileType.Letter);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DecodeString_EmptyArray_ReturnsEmptyString()
        {
            Utilities.DecodeString(new byte[0]).Should().BeBlank();
        }

        [Test]
        public void EncodeString_EmptyString_ReturnsZeroLengthByArray()
        {
            Utilities.EncodeString("").Should().Equal(new byte[0]);
        }

        [Test]
        public void FileTypeString_FileTypeHelp_ReturnsHelp()
        {
            Utilities.FileTypeString(FileType.Help).Should().Be("Help");
        }

        [Test]
        public void FileTypeString_FileTypeLetter_ReturnsLetter()
        {
            Utilities.FileTypeString(FileType.Letter).Should().Be("Letter");
        }

        [Test]
        public void FileTypeString_FileTypePowerPlus_ReturnsRepWriter()
        {
            Utilities.FileTypeString(FileType.PowerPlus).Should().Be("RepWriter");
        }

        [Test]
        public void FileTypeString_FileTypeRepGen_ReturnsRepWriter()
        {
            Utilities.FileTypeString(FileType.RepGen).Should().Be("RepWriter");
        }

        [Test]
        public void FileTypeString_FileTypeReport_ReturnsReport()
        {
            Utilities.FileTypeString(FileType.Report).Should().Be("Report");
        }

        [Test]
        public void ParseSystemTime_1DigitMonthDate_ReturnsCorrectly()
        {
            var expected = new DateTime(2001, 1, 1);
            DateTime actual = Utilities.ParseSystemTime("1012001", "");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ParseSystemTime_BlankDate_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => { DateTime date = Utilities.ParseSystemTime("", ""); }
                );
        }

        [Test]
        public void ParseSystemTime_BlankTime_ReturnsMidnightOnDate()
        {
            var expected = new DateTime(2001, 1, 1);
            DateTime actual = Utilities.ParseSystemTime("01012001", "");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ParseSystemTime_NullDate_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => { DateTime date = Utilities.ParseSystemTime(null, ""); }
                );
        }

        [Test]
        public void ParseSystemTime_NullTime_ReturnsMidnightOnDate()
        {
            var expected = new DateTime(2001, 1, 1);
            DateTime actual = Utilities.ParseSystemTime("01012001", null);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ParseSystemTime_ReallyShortDate_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utilities.ParseSystemTime("2001", "01"));
        }

        [Test]
        public void ParseSystemTime_ShortTime_ReturnsZeroHourAndCorrectMinute()
        {
            var expected = new DateTime(2001, 1, 1, 0, 1, 0);
            DateTime actual = Utilities.ParseSystemTime("01012001", "01");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ConvertTimeStringToInt_NullParameter_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => Utilities.ConvertTime(null));
        }

        [Test]
        public void ConvertTimeStringToInt_EmptyParameter_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => Utilities.ConvertTime(""));
        }

        [Test]
        public void ConvertTimeStringToInt_InvalidFormat_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => Utilities.ConvertTime("00"));
        }

        [Test]
        public void ConvertTimeStringToInt_ValidFormatAllZeros_ReturnsZero()
        {
            var time = Utilities.ConvertTime("00:00:00");

            time.Should().Be(0);
        }

        [Test]
        public void ConvertTimeDateToInt_Midnight_ReturnsZero()
        {
            var time = Utilities.ConvertTime(DateTime.Today);
            time.Should().Be(0);
        }

        [Test]
        public void ConvertTimeDateToInt_Noon_HasCorrectValue()
        {
            var time = Utilities.ConvertTime(DateTime.Today.AddHours(12));
            time.Should().Be(43200);
        }

        [Test]
        public void ConvertTimeIntToDate_NegativeParameter_ThrowsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utilities.ConvertTime(-1));
        }

        [Test]
        public void ConvertTimeIntToDate_Zero_ReturnsMidnight()
        {
            var expected = DateTime.Today;
            var time = Utilities.ConvertTime(0);
            time.Should().Be(expected);
        }

        [Test]
        public void ConvertTimeIntToDate_NoonSeconds_HasCorrectValue()
        {
            var expected = DateTime.Today.AddHours(12);
            var time = Utilities.ConvertTime(43200);
            time.Should().Be(expected);
        }
    }
}