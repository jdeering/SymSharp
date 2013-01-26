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
    }
}
