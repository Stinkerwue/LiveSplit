﻿using LiveSplit.Model.Comparisons;
using LiveSplit.Options;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LiveSplit.Model.RunFactories
{
    public class TimeSplitTrackerRunFactory : IRunFactory
    {
        public string Path { get; set; }
        public Stream Stream { get; set; }

        public TimeSplitTrackerRunFactory(Stream stream = null, string path = null)
        {
            Stream = stream;
            Path = path;
        }

        TimeSpan? parseTimeNullable(string timeString)
        {
            var time = TimeSpanParser.Parse(timeString);
            return (time == TimeSpan.Zero) ? (TimeSpan?)null : time;
        }

        public IRun Create(IComparisonGeneratorsFactory factory)
        {
            string path = "";
            if (!string.IsNullOrEmpty(Path))
                path = System.IO.Path.GetDirectoryName(Path);

            var run = new Run(factory);

            var reader = new StreamReader(Stream);

            var line = reader.ReadLine();
            var titleInfo = line.Split('\t');
            run.AttemptCount = int.Parse(titleInfo[0]);
            run.Offset = TimeSpanParser.Parse(titleInfo[1]);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    run.GameIcon = Image.FromFile(System.IO.Path.Combine(path, titleInfo[2]));
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            line = reader.ReadLine();
            titleInfo = line.Split('\t');
            run.CategoryName = titleInfo[0];
            var comparisons = titleInfo.Skip(2).ToArray();

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length <= 0 || string.IsNullOrWhiteSpace(line))
                    continue;

                var segment = new Segment("");

                var segmentInfo = line.Split('\t');

                segment.Name = segmentInfo[0];
                Time newBestSegment = new Time();
                newBestSegment.RealTime = parseTimeNullable(segmentInfo[1]);
                segment.BestSegmentTime = newBestSegment;
                Time pbTime = new Time();
                for (var i = 0; i < comparisons.Length; ++i)
                {
                    Time newComparison = new Time(segment.Comparisons[comparisons[i]]);
                    newComparison.RealTime = pbTime.RealTime = parseTimeNullable(segmentInfo[i + 2]);
                    segment.Comparisons[comparisons[i]] = newComparison;
                }
                segment.PersonalBestSplitTime = pbTime;

                line = reader.ReadLine();

                if (line.Length > 0 && !string.IsNullOrWhiteSpace(line) && !string.IsNullOrEmpty(path))
                {
                    try
                    {
                        segment.Icon = Image.FromFile(System.IO.Path.Combine(path, line.Split('\t')[0]));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                run.Add(segment);
            }

            foreach (var comparison in comparisons)
                run.CustomComparisons.Add(comparison);

            return run;
        }
    }
}
