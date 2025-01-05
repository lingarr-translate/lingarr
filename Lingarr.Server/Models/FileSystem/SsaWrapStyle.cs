namespace Lingarr.Server.Models.FileSystem;

public enum SsaWrapStyle
{
    /// <summary>Smart wrapping, lines are evenly broken</summary>
    Smart = 0,
    /// <summary>End-of-line word wrapping, only \N breaks</summary>
    EndOfLine = 1,
    /// <summary>No word wrapping, \n \N both break</summary>
    None = 2,
    /// <summary>Same as Smart, but lower line gets wider</summary>
    SmartWideLowerLine = 3
}