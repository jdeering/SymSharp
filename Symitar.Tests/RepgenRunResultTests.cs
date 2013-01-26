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
    public class RepgenRunResultTests
    {
        [Test]
        public void RepgenRunResult_Constructor_DefaultsSequenceToNegativeOne()
        {
            RepgenRunResult result = new RepgenRunResult();
            result.Sequence.Should().Be(-1);
        }

        [Test]
        public void RepgenRunResult_Constructor_DefaultsRunTimeToNegativeOne()
        {
            RepgenRunResult result = new RepgenRunResult();
            result.RunTime.Should().Be(-1);
        }

        [Test]
        public void RepgenRunResult_Okay_HasCorrectRunStatus()
        {
            RepgenRunResult result = RepgenRunResult.Okay(0, 0);
            result.Status.Should().Be(RunStatus.Okay);
        }

        [Test]
        public void RepgenRunResult_Okay_HasCorrectRunTime()
        {
            RepgenRunResult result = RepgenRunResult.Okay(0, 200);
            result.RunTime.Should().Be(200);
        }

        [Test]
        public void RepgenRunResult_Okay_HasCorrectSequence()
        {
            RepgenRunResult result = RepgenRunResult.Okay(100, 0);
            result.Sequence.Should().Be(100);
        }

        [Test]
        public void RepgenRunResult_Cancelled_HasCorrectRunStatus()
        {
            RepgenRunResult result = RepgenRunResult.Cancelled();
            result.Status.Should().Be(RunStatus.Cancelled);
        }

        [Test]
        public void RepgenRunResult_ErrorWithNullMessage_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    RepgenRunResult result = RepgenRunResult.Error(null);
                }
            );
        }

        [Test]
        public void RepgenRunResult_ErrorWithBlankMessage_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    RepgenRunResult result = RepgenRunResult.Error("");
                }
            );
        }

        [Test]
        public void RepgenRunResult_Error_HasCorrectRunStatus()
        {
            RepgenRunResult result = RepgenRunResult.Error("Error");
            result.Status.Should().Be(RunStatus.Error);
        }

        [Test]
        public void RepgenRunResult_Error_HasCorrectErrorMessageWhenNotBlank()
        {
            RepgenRunResult result = RepgenRunResult.Error("This is an error message.");
            result.ErrorMessage.Should().Be("This is an error message.");
        }

        [Test]
        public void RepgenRunResult_FileNotFound_HasCorrectRunStatus()
        {
            RepgenRunResult result = RepgenRunResult.FileNotFound();
            result.Status.Should().Be(RunStatus.FileNotFound);
        }
    }
}
