using System;
using FluentAssertions;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class ReportInfoTests
    {
        [Test]
        public void Constructor_NegativeSequence_ThrowsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReportInfo(-1, ""));
        }

        [Test]
        public void Constructor_NullTitle_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ReportInfo(1, null));
        }

        [Test]
        public void Constructor_ValidParameters_SetsCorrectSequence()
        {
            var reportInfo = new ReportInfo(100, "DQ.REPORT");

            reportInfo.Sequence.Should().Be(100);
        }

        [Test]
        public void Constructor_ValidParameters_SetsCorrectTitle()
        {
            var reportInfo = new ReportInfo(100, "DQ.REPORT");

            reportInfo.Title.Should().Be("DQ.REPORT");
        }

        [Test]
        public void Constructor_ZeroSequence_ThrowsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReportInfo(0, ""));
        }
    }
}