using System.Text.Json.Serialization;

namespace TestTask.Models
{
    public class RequestModel
    {
        public string? Selector { get; set; }
        public string? Attribute { get; set; }
        [JsonPropertyName("url_b64")]
        public string? UrlBase64 { get; set; }
        [JsonPropertyName("encrypted_text_bytes_b64")]
        public string? EncryptedTextBytesBase64 { get; set; }
        [JsonPropertyName("key_bytes_b64")]
        public string? KeyBytesBase64 { get; set; }
        [JsonPropertyName("page_b64")]
        public string? PageBase64 { get; set; }
    }
}
