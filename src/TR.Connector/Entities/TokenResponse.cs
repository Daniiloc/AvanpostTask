using System.Text.Json.Serialization;

namespace TR.Connector.Entities
{
    public class TokenResponse : BaseResponse<TokenResponseData>
    {
    }

    public class TokenResponseData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
