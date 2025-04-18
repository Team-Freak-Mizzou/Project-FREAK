using System;
using System.Threading;
using ScottPlot.WPF;
using Project_FREAK.Controllers;
using Xunit;

namespace Project_FREAK.Tests
{
    public class GraphManagerTests
    {
        private void RunInSta(Action action)
        {
            Exception ex = null!;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception e) { ex = e; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (ex != null) throw ex;
        }

        [Fact]
        public void Construct_DoesNotThrow()
        {
            RunInSta(() =>
            {
                var gm = new GraphManager(new WpfPlot(), new WpfPlot());
            });
        }

        [Fact]
        public void AddDataPoint_DoesNotThrow()
        {
            RunInSta(() =>
            {
                var gm = new GraphManager(new WpfPlot(), new WpfPlot());
                gm.AddDataPoint(0, 1, 2);
            });
        }

        [Fact]
        public void ClearGraphs_DoesNotThrow()
        {
            RunInSta(() =>
            {
                var gm = new GraphManager(new WpfPlot(), new WpfPlot());
                gm.ClearGraphs();
            });
        }
    }
}
