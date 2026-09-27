using System.Text.Json.Serialization;

namespace TestJob.Models;

public record RequestDto(
    [property: JsonPropertyName("selector")] string Selector,
    [property: JsonPropertyName("attribute")] string Attribute,
    [property: JsonPropertyName("url_b64")] string UrlB64,
    [property: JsonPropertyName("encrypted_text_bytes_b64")] string EncryptedTextBytesB64,
    [property: JsonPropertyName("key_bytes_b64")] string KeyBytesB64,
    [property: JsonPropertyName("page_b64")] string PageB64);
