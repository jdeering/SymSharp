using System;
using FluentAssertions;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class FileTests
    {
        [Test]
        public void Constructor_BasicParameters_HasCorrectFileSize()
        {
            var file = new File("", "", "", FileType.RepGen, DateTime.Now, 101);
            file.Size.Should().Be(101);
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectFileType()
        {
            var file = new File("", "", "", FileType.Letter, DateTime.Now, 101);
            file.Type.Should().Be(FileType.Letter);
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectName()
        {
            var file = new File("", "", "RANDOM.FILE.NAME", FileType.RepGen, DateTime.Now, 101);
            file.Name.Should().Be("RANDOM.FILE.NAME");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectServer()
        {
            var file = new File("symitar", "", "", FileType.RepGen, DateTime.Now, 101);
            file.Server.Should().Be("symitar");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectSym()
        {
            var file = new File("", "10", "", FileType.RepGen, DateTime.Now, 101);
            file.Sym.Should().Be("10");
        }

        [Test]
        public void Constructor_BasicParameters_HasCorrectType()
        {
            var file = new File("", "", "", FileType.RepGen, DateTime.Now, 101);
            file.Size.Should().Be(101);
        }

        [Test]
        public void Constructor_NoParameters_DefaultsFileSize0()
        {
            var file = new File();
            file.Size.Should().Be(0);
        }

        [Test]
        public void Constructor_NoParameters_DefaultsRepgenFileType()
        {
            var file = new File();
            file.Type.Should().Be(FileType.RepGen);
        }
    }
}