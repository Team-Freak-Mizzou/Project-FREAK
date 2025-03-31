using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Collections.Generic;

namespace Project_FREAK.Controllers
{
    public class GraphManager
    {
        private readonly DataLogger _thrustLogger;
        private readonly DataLogger _pressureLogger;
        private readonly WpfPlot _thrustGraph;
        private readonly WpfPlot _pressureGraph;
        private const double WindowSize = 10; // Size of the window for displaying data

        public GraphManager(WpfPlot thrustGraph, WpfPlot pressureGraph)
        {
            _thrustGraph = thrustGraph;
            _pressureGraph = pressureGraph;

            // Initialize the graphs with titles and labels
            _thrustLogger = InitializeGraph(_thrustGraph, "Thrust Over Time", "Thrust (N)");
            _pressureLogger = InitializeGraph(_pressureGraph, "Pressure Over Time", "Pressure (PSI)");
        }

        // Initializes a graph with a title and y-axis label
        private DataLogger InitializeGraph(WpfPlot graph, string title, string yLabel)
        {
            graph.Plot.Title(title);
            graph.Plot.Axes.Bottom.Label.Text = "Time (s)";
            graph.Plot.Axes.Left.Label.Text = yLabel;
            graph.Plot.Axes.AutoScaleY(); // Enable auto-scaling for the y-axis

            var logger = graph.Plot.Add.DataLogger();
            logger.ViewSlide(WindowSize); // Set the view window size
            return logger;
        }

        // Adds a data point to the thrust and pressure graphs
        public void AddDataPoint(double time, double thrust, double pressure)
        {
            _thrustLogger.Add(time, thrust);
            _pressureLogger.Add(time, pressure);
            RefreshGraphs(); // Refresh the graphs to display the new data
        }

        // Refreshes the graphs to display the latest data
        public void RefreshGraphs()
        {
            _thrustGraph.Refresh();
            _pressureGraph.Refresh();
        }

        // Clears the data from the graphs
        public void ClearGraphs()
        {
            _thrustLogger.Clear();
            _pressureLogger.Clear();
            RefreshGraphs();
        }
    }
}
