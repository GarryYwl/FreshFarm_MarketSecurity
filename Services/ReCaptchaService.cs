using System.Text.Json;

namespace FreshFarmMarketSecurity.Services
{
    public class ReCaptchaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ReCaptchaService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<(bool ok, string reason, double score)> VerifyAsync(string token, string expectedAction)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Missing reCAPTCHA token.", 0);

            var secret = _config["GoogleReCaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return (false, "reCAPTCHA secret key not configured.", 0);

            var client = _httpClientFactory.CreateClient();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token
            });

            var resp = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            if (!resp.IsSuccessStatusCode)
                return (false, "reCAPTCHA verification request failed.", 0);

            var json = await resp.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ReCaptchaVerifyResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null)
                return (false, "Invalid reCAPTCHA response.", 0);

            if (!result.Success)
                return (false, "reCAPTCHA failed.", result.Score);

            // Optional safety checks
            if (!string.IsNullOrWhiteSpace(expectedAction) && !string.Equals(result.Action, expectedAction, StringComparison.Ordinal))
                return (false, "reCAPTCHA action mismatch.", result.Score);

            var minScoreStr = _config["GoogleReCaptcha:MinimumScore"];
            var minScore = 0.5;
            if (double.TryParse(minScoreStr, out var parsed)) minScore = parsed;

            if (result.Score < minScore)
                return (false, $"Low reCAPTCHA score ({result.Score:0.00}).", result.Score);

            return (true, "OK", result.Score);
        }

        private class ReCaptchaVerifyResponse
        {
            public bool Success { get; set; }
            public double Score { get; set; }
            public string Action { get; set; } = "";
            public DateTime Challenge_Ts { get; set; }
            public string Hostname { get; set; } = "";
            public string[]? Error_Codes { get; set; }
        }
    }
}
