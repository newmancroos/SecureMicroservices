namespace Movies.Client.Models;
public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Genre { get; set; } = default!;
    public string Rating { get; set; } = default!;
    public DateTime ReleaseDate { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Owner { get; set; } = default!;
}

