using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class FileTests
    {
        [Test]
        public void Constructor_NoParameters_DefaultsRepgenFileType()
        {
            File file = new File();
            file.Type.Should().Be(FileType.RepGen);
        }

        [Test]
        public void Constructor_NoParameters_DefaultsTimeNow()
        {
            File file = new File();
            file.TimeStamp.Should().Be(DateTime.Now);
        }

        [Test]
        public void Constructor_NoParameters_DefaultsFileSize0()
        {
            File file = new File();
            file.FileSize.Should().Be(0);
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectFileSize()
        {
            File file = new File("", "", "", FileType.RepGen, DateTime.Now, 101);
            file.FileSize.Should().Be(101);
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectType()
        {
            File file = new File("", "", "", FileType.RepGen, DateTime.Now, 101);
            file.FileSize.Should().Be(101);
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectServer()
        {
            File file = new File("symitar", "", "", FileType.RepGen, DateTime.Now, 101);
            file.Server.Should().Be("symitar");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectSym()
        {
            File file = new File("", "10", "", FileType.RepGen, DateTime.Now, 101);
            file.Sym.Should().Be("10");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectName()
        {
            File file = new File("", "", "RANDOM.FILE.NAME", FileType.RepGen, DateTime.Now, 101);
            file.FileName.Should().Be("RANDOM.FILE.NAME");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectFileType()
        {
            File file = new File("", "", "", FileType.Letter, DateTime.Now, 101);
            file.Type.Should().Be(FileType.Letter);
        }
    }
}
