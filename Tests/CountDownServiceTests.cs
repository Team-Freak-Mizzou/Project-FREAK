using System;
using System.Reflection;
using System.Windows.Threading;
using Project_FREAK.Controllers;
using Xunit;


namespace Project_FREAK.Tests.Tests
{
    public class CountdownServiceTests
    {
        [Fact]
        public void ToggleCountdown_StartsTimerAndFiresUpdated()
        {
            var svc = new CountdownService(Dispatcher.CurrentDispatcher);
            svc.InitializeTimer(TimeSpan.Zero);
            string? last = null;
            svc.CountdownUpdated += t => last = t;

            svc.ToggleCountdown(3);
            Assert.Equal("00:3", last);

            var m = svc.GetType()
                       .GetMethod("Timer_Tick", BindingFlags.NonPublic | BindingFlags.Instance)!;
            for (int i = 0; i < 2; i++)
                m.Invoke(svc, new object?[] { null, EventArgs.Empty });

            Assert.Equal("00:1", last);
        }

        [Fact]
        public void Countdown_FiresFinishedEvent()
        {
            var svc = new CountdownService(Dispatcher.CurrentDispatcher);
            svc.InitializeTimer(TimeSpan.Zero);
            bool done = false;
            svc.CountdownFinished += () => done = true;

            svc.ToggleCountdown(1);
            var m = svc.GetType()
                       .GetMethod("Timer_Tick", BindingFlags.NonPublic | BindingFlags.Instance)!;
            m.Invoke(svc, new object?[] { null, EventArgs.Empty });

            Assert.True(done);
        }
    }
}
