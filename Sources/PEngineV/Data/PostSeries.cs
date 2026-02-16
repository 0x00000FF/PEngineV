namespace PEngineV.Data;

public class PostSeries
{
    public int PostId { get; set; }
    public int SeriesId { get; set; }

    // Order of the post within the series
    public int OrderIndex { get; set; }

    public Post Post { get; set; } = null!;
    public Series Series { get; set; } = null!;
}
