using System;
using dpp.cot;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class UiEventMessageTests
    {
        [Fact]
        public void UiEventMessageProjectsCommonMapFields()
        {
            var evt = new Event
            {
                Uid = "TEST-UID",
                Type = "a-f-G-U-C",
                How = "m-g",
                Time = new DateTime(2022, 2, 2, 22, 22, 22, DateTimeKind.Utc),
                Stale = new DateTime(2022, 2, 2, 22, 32, 22, DateTimeKind.Utc),
                Point = new Point { Lat = 34.1, Lon = -117.2, Hae = 0, Ce = 1, Le = 1 },
                Detail = new Detail
                {
                    Contact = new Contact { Callsign = "VIPER" }
                }
            };

            var message = UiEventMessage.FromEvent(evt, "peer:test");

            Assert.Equal("TEST-UID", message.Uid);
            Assert.Equal("a-f-G-U-C", message.Type);
            Assert.Equal(34.1, message.Lat);
            Assert.Equal(-117.2, message.Lon);
            Assert.Equal("VIPER", message.Callsign);
            Assert.Equal("peer:test", message.SourceId);
        }
    }
}
