using HtmlAgilityPack;

namespace CosmicBot.Helpers
{
    public static class BedrockXUIDHelper
    {
        public async static Task<string> GetXUID(string username)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var formResponsePage = await client.GetAsync("https://www.cxkes.me/xbox/xuid");
            formResponsePage.EnsureSuccessStatusCode();
            var formResponseHtml = await formResponsePage.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(formResponseHtml);
            var tokenNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='_token']");
            if (tokenNode == null)
                throw new Exception("Token not found");
            string tokenValue = tokenNode.GetAttributeValue("value", string.Empty);

            var xuidResponse = await client.PostAsync("https://www.cxkes.me/xbox/xuid", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "_token", tokenValue },
                { "gamertag", username }
            }));
            xuidResponse.EnsureSuccessStatusCode();
            var xuidResponseHtml = await xuidResponse.Content.ReadAsStringAsync();
            var resultDoc = new HtmlDocument();
            resultDoc.LoadHtml(xuidResponseHtml);
            var xuidNode = resultDoc.DocumentNode.SelectSingleNode("//code[@id='xuidHex']");
            var xuid = xuidNode.InnerText.Trim();
            return $"00000000-0000-0000-{xuid.Substring(0, 4)}-{xuid.Substring(4)}";
        }

    }
}
