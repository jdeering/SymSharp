using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Symitar.Tests
{
    [TestFixture]
    public class SymSocketTests
    {
        [Test]
        public void ReadUntilMultiMatch_EmptyMatcherSet_ThrowsArugmentException()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    {
                        var socket = new SymSocket();

                        socket.ReadUntil(new List<byte[]>(), 1);
                    }
                );
        }

        [Test]
        public void ReadUntilMultiMatch_ZeroTimeout_ThrowsArugmentException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    var socket = new SymSocket();

                    socket.ReadUntil(new List<byte[]>(){ new byte[1] }, 0);
                }
                );
        }
    }
}
