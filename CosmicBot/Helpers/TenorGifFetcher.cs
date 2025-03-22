using System.Net.Http.Json;

namespace CosmicBot.Helpers
{
    public static class TenorGifFetcher
    {
        private static string _key = "dlNfvgmQL6Am4Leym7p3tX3oDgeni8qA";
        private static Uri _baseAddress = new Uri("https://api.giphy.com/v1/gifs/");

        private class GifResponse
        {
            public GifObject? Data { get; set; }
        }

        private class GifObject
        {
            public string Url { get; set; } = string.Empty;
            public string Embed_url { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public ImagesObject? Images { get; set; }

        }

        private class ImagesObject
        {
            public RenditionObject? Fixed_height { get; set; }
        }

        private class RenditionObject
        {
            public string Url { get; set; } = string.Empty;
        }
        public async static Task<string> GetRandomGifUrl(string query)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = _baseAddress;
                var response = await httpClient.GetAsync($"random?api_key={_key}&tag={query}&rating=g");
                if (!response.IsSuccessStatusCode)
                    return string.Empty;

                var gif = await response.Content.ReadFromJsonAsync<GifResponse>();
                if(gif == null) 
                    return string.Empty;

                return gif.Data?.Images?.Fixed_height?.Url ?? string.Empty;
            }
        }
    }
}
