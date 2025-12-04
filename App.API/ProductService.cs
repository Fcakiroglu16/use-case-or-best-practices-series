namespace App.API
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _urls = new[]
        {
            "https://www.google.com",
            "https://www.github.com",
            "https://www.microsoft.com"
        };

        public ProductService()
        {
            _httpClient = new HttpClient();
        }

        public List<int> GetContentLengths()
        {
            var lengths = new List<int>();

            foreach (var url in _urls)
            {
                var response = _httpClient.GetAsync(url).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                lengths.Add(content.Length);
            }

            return lengths;
        }

        public async IAsyncEnumerable<int> GetContentLengthsAsync()
        {
            foreach (var url in _urls)
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                yield return content.Length;
            }
        }
    }
}
