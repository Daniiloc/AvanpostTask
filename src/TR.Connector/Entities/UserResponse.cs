using System.Text.Json.Serialization;

namespace TR.Connector.Entities
{
    public class UserResponseData
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("status")] 
        public string Status { get; set; }
    }

    class UserResponse : BaseResponse<List<UserResponseData>>
    {
    }
}
