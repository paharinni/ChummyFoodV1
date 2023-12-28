namespace ChummyFoodBack.Factories
{
    public class CoinbaseHttpClientFactory
    {
        public static void AddCoinbaseSecurity(string apiKey, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders
                    .Add("X-CC-Api-Key", apiKey);
            httpClient.DefaultRequestHeaders
                .Add("X-CC-Version", "2018-03-22");
        }
    }
}
