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
    public class SymCommandTests
    {
        // Added this as a local counter to match
        // the static counter in SymCommand without
        // having to manipulate it every time.
        private static int ExpectedId = 9999;

        [SetUp]
        public void UpdateMsgIdCounter()
        {
            ExpectedId++;
        }

        [Test]
        public void Constructor_NoArguments_ShouldHaveParameterMsgId()
        {
            var cmd = new SymCommand();
            cmd.HasParameter("MsgId").Should().BeTrue();
        }

        [Test]
        public void Constructor_WithCommand_ShouldHaveParameterMsgId()
        {
            var cmd = new SymCommand("Command");
            cmd.HasParameter("MsgId").Should().BeTrue();
        }

        [Test]
        public void Constructor_WithCommand_ShouldHaveCorrectCommand()
        {
            var cmd = new SymCommand("Command");
            cmd.Command.Should().Be("Command");
        }

        [Test]
        public void Constructor_WithCommandAndParam_ShouldHaveParameterMsgId()
        {
            var parameters = new Dictionary<string, string> { { "Initial", "1" } };
            var cmd = new SymCommand("Command", parameters);

            cmd.HasParameter("MsgId").Should().BeTrue();
        }

        [Test]
        public void Constructor_WithCommandAndParam_ShouldHaveCorrectCommand()
        {
            var parameters = new Dictionary<string, string> { { "Initial", "1" } };
            var cmd = new SymCommand("Command", parameters);

            cmd.Command.Should().Be("Command");
        }

        [Test]
        public void Constructor_WithCommandAndParam_ShouldHaveTheParameter()
        {
            var parameters = new Dictionary<string, string> { { "Initial", "1" } };
            var cmd = new SymCommand("Command", parameters);

            cmd.HasParameter("Initial").Should().BeTrue();
        }

        [Test]
        public void Constructor_WithCommandAndParam_ShouldHaveTheCorrectParameterValue()
        {
            var parameters = new Dictionary<string, string> {{"Initial", "1"}};
            var cmd = new SymCommand("Command", parameters);

            cmd.Get("Initial").Should().Be("1");
        }

        [Test]
        public void Constructor_WithCommandParamAndData_ShouldHaveData()
        {
            var parameters = new Dictionary<string, string> { { "Initial", "1" } };
            var cmd = new SymCommand("Command", parameters, "data");

            cmd.Data.Should().NotBeBlank();
        }

        [Test]
        public void Constructor_WithCommandParamAndData_ShouldHaveCorrectData()
        {
            var parameters = new Dictionary<string, string> { { "Initial", "1" } };
            var cmd = new SymCommand("Command", parameters, "data");

            cmd.Data.Should().Be("data");
        }

        [Test]
        public void Parse_WithNoTildes_ShouldHaveHaveBlankData()
        {
            var cmd = SymCommand.Parse("CommandMessage");
            cmd.Data.Should().BeBlank();
        }

        [Test]
        public void Parse_WithNoTildes_ShouldHaveHaveOneParameter()
        {
            var cmd = SymCommand.Parse("CommandMessage");
            cmd.Parameters.Count.Should().Be(1);
        }

        [Test]
        public void Parse_WithNoTildes_ShouldHaveCorrectCommand()
        {
            var cmd = SymCommand.Parse("CommandMessage");
            cmd.Command.Should().Be("CommandMessage");
        }

        [Test]
        public void Parse_WithParameter_ShouldHaveEmptyData()
        {
            var cmd = SymCommand.Parse("CommandMessage");
            cmd.Data.Should().BeBlank();
        }

        [Test]
        public void Parse_WithParameter_ShouldHave2Parameters()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=RandomValue");
            cmd.Parameters.Count.Should().Be(2);
        }

        [Test]
        public void Parse_WithParameter_ShouldHaveTheSpecifiedParameter()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=RandomValue");
            cmd.HasParameter("Parameter1").Should().BeTrue();
        }

        [Test]
        public void Parse_WithParameter_ShouldHaveTheCorrectParameterValue()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=RandomValue");
            cmd.Get("Parameter1").Should().Be("RandomValue");
        }

        [Test]
        public void Parse_WithParameter_ShouldHaveCorrectCommand()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=RandomValue");
            cmd.Command.Should().Be("CommandMessage");
        }

        [Test]
        public void Parse_WithParameterWithNoValue_ShouldHaveParameter()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1");
            cmd.HasParameter("Parameter1").Should().BeTrue();
        }

        [Test]
        public void Parse_WithParameterWithNoValue_ShouldHaveParameterValueBlank()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1");
            cmd.Get("Parameter1").Should().BeBlank();
        }

        [Test]
        public void ToString_WithNoAddedParameters_ShouldBeCorrect()
        {
            var cmd = SymCommand.Parse("CommandMessage");
            cmd.ToString().Should().Be("\u000726\rCommandMessage~MsgId=" + ExpectedId);
        }

        [Test]
        public void ToString_WithAddedParameter_ShouldBeCorrect()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=Value");
            cmd.ToString().Should().Be("\u000743\rCommandMessage~MsgId="+ExpectedId+"~Parameter1=Value");
        }

        [Test]
        public void ToString_WithAddedParameterMissingValue_ShouldBeCorrect()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1");
            cmd.ToString().Should().Be("\u000737\rCommandMessage~MsgId=" + ExpectedId + "~Parameter1");
        }

        [Test]
        public void Set_NewParameter_ParameterShouldExist()
        {
            var cmd = new SymCommand();
            cmd.Set("Parameter3", "RandomValue");
            cmd.Parameters.ContainsKey("Parameter3").Should().BeTrue();
        }

        [Test]
        public void Set_NewParameter_ParameterShouldHaveCorrectValue()
        {
            var cmd = new SymCommand();
            cmd.Set("Parameter3", "RandomValue");
            cmd.Parameters["Parameter3"].Should().Be("RandomValue");
        }

        [Test]
        public void Set_ExistingParameter_ParameterShouldHaveNewValue()
        {
            var cmd = SymCommand.Parse("CommandMessage~Parameter1=Value");
            cmd.Set("Parameter1", "NewValue");
            cmd.Parameters["Parameter1"].Should().Be("NewValue");
        }

        [Test]
        public void GetFileData_EmptyData_ShouldReturnEmptyString()
        {
            var cmd = new SymCommand();

            cmd.GetFileData().Should().BeBlank();
        }

        [Test]
        public void GetFileData_IncorrectFormattedData_ShouldReturnEmptyString()
        {
            var cmd = new SymCommand();

            cmd.Data = "RandomData";

            cmd.GetFileData().Should().BeBlank();
        }

        [Test]
        public void GetFileData_DataMissingFirstDelimiter_ShouldReturnEmptyString()
        {
            var cmd = new SymCommand();

            cmd.Data = "File Data\u00FE";

            cmd.GetFileData().Should().BeBlank();
        }

        [Test]
        public void GetFileData_DataMissingSecondDelimiter_ShouldReturnEmptyString()
        {
            var cmd = new SymCommand();

            cmd.Data = "\u00FDFile Data";

            cmd.GetFileData().Should().BeBlank();
        }

        [Test]
        public void GetFileData_NoDataBetweenDelimiters_ShouldReturnEmptyString()
        {
            var cmd = new SymCommand();

            cmd.Data = "\u00FD\u00FE";

            cmd.GetFileData().Should().BeBlank();
        }

        [Test]
        public void GetFileData_DataWithIncorrectDelimiterOrder_ShouldThrowException()
        {
            var cmd = new SymCommand();

            cmd.Data = "\u00FEFile Data\u00FD";

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    cmd.GetFileData();
                });
        }

        [Test]
        public void GetFileData_CorrectFormatData_ShouldReturnCorrectFileData()
        {
            var cmd = new SymCommand();

            cmd.Data = "\u00FDFile Data\u00FE";

            cmd.GetFileData().Should().Be("File Data");
        }
    }
}
