using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleChattyServer
{
    public sealed class StepStopwatch
    {
        private Stopwatch _overallStopwatch = Stopwatch.StartNew();
        private Stopwatch _stepStopwatch = Stopwatch.StartNew();
        private string _name;
        private List<(string Name, TimeSpan Time)> _steps = new List<(string Name, TimeSpan Time)>();

        public TimeSpan Elapsed => _overallStopwatch.Elapsed;

        public void Step(string name)
        {
            if (_name != null)
                _steps.Add((_name, _stepStopwatch.Elapsed));
            _name = name;
            _stepStopwatch.Restart();
        }

        public override string ToString()
        {
            Step(null);
            return $"{_overallStopwatch.Elapsed.TotalMilliseconds:#,##0} ms (" +
                string.Join(", ", _steps.Select(x => $"{x.Name} {x.Time.TotalMilliseconds:#,##0} ms")) +
                ")";
        }
    }
}
