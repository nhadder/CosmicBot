using Newtonsoft.Json;

namespace CosmicBot.Models
{
    public class RedditData<T>
    {
        public T Data { get; set; }
    }

    public class RedditListingData
    {
        public List<RedditData<RedditPostData>> Children { get; set; }
        public RedditListingData()
        {
            Children = new List<RedditData<RedditPostData>>();
        }
    }

    public class RedditPostData
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Selftext { get; set; }
        public string? Thumbnail { get; set; }
        public bool? Is_gallery { get; set; }
        public RedditPreview? Preview { get; set; }
        public RedditVideo? Media { get; set; }
        [JsonIgnore]
        public bool IsImage => Url != null && (Url.EndsWith(".jpg") || Url.EndsWith(".png") || Url.EndsWith(".gif") || Url.EndsWith(".gifv") || Url.EndsWith(".jpeg"));
    }

    public class RedditVideo
    {
        public RedditVideoPreview? Reddit_video { get; set; }
    }

    public class RedditPreview
    {
        public RedditVideoPreview? Reddit_video_preview { get; set; }
    }

    public class RedditVideoPreview
    {
        public string? Fallback_url { get; set; }
    }
}
