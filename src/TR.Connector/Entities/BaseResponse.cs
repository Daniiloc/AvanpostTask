using System.Text.Json.Serialization;

namespace TR.Connector.Entities
{
    public abstract class BaseResponse<T>
    {
        [JsonPropertyName("data")]
        public T? ResponseData { get; set; }
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }
        [JsonPropertyName("errorText")]
        public string? ErrorText { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
