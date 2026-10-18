using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.SamHomeSections.Model.Dto
{
    public abstract class ArrDtoBase
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("images")]
        public ArrImageDto[]? Images { get; set; }
    }
}