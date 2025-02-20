using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookPostData {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("user")]
    public NeobookUser User { get; set; }
    
    [JsonPropertyName("chapters")]
    public NeobookChapter[] Chapters { get; set; }
}