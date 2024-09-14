﻿namespace Lingarr.Server.Models.FileSystem;

public class SubtitleItem
{
    /// <summary>
    /// Position
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Start time in milliseconds.
    /// </summary>
    public int StartTime { get; set; }

    /// <summary>
    /// End time in milliseconds.
    /// </summary>
    public int EndTime { get; set; }

    /// <summary>
    /// The raw subtitle string from the file
    /// May include formatting
    /// </summary>
    public List<string> Lines { get; set; }

    /// <summary>
    /// The plain-text string from the file
    /// Does not include formatting
    /// </summary>
    public List<string> PlaintextLines { get; set; }

    /// <summary>
    /// The empty constructor
    /// </summary>
    public SubtitleItem()
    {
        this.Lines = new List<string>();
        this.PlaintextLines = new List<string>();
    }

    public override string ToString()
    {
        var startTs = new TimeSpan(0, 0, 0, 0, StartTime);
        var endTs = new TimeSpan(0, 0, 0, 0, EndTime);

        var res = string.Format("{0} --> {1}: {2}", startTs.ToString("G"), endTs.ToString("G"),
            string.Join(Environment.NewLine, Lines));
        return res;
    }
}