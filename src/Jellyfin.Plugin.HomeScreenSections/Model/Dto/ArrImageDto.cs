using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.SamHomeSections.Model.Dto
{
    public class ArrImageDto
    {
        [JsonPropertyName("coverType")]
        public string? CoverType { get; set; }

        [JsonPropertyName("remoteUrl")]
        public string? RemoteUrl { get; set; }
    }
}