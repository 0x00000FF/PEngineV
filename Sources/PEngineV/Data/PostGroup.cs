namespace PEngineV.Data;

public class PostGroup
{
    public int PostId { get; set; }
    public int GroupId { get; set; }

    public Post Post { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
