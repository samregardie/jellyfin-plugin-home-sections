using System.Net.Http.Json;
using Jellyfin.Plugin.SamHomeSections.Configuration;
using Jellyfin.Plugin.SamHomeSections.Library;
using Jellyfin.Plugin.SamHomeSections.Model.Dto;
using Jellyfin.Plugin.SamHomeSections.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.SamHomeSections.HomeScreen.Sections
{
    public class DiscoverTvSection : DiscoverSection
    {
        public override string? Section => "DiscoverTV";

        public override string? DisplayText { get; set; } = "Discover TV Shows";
        
        protected override string JellyseerEndpoint => "/api/v1/discover/tv";
        
        public DiscoverTvSection(IUserManager userManager, ImageCacheService imageCacheService) 
            : base(userManager, imageCacheService)
        {
        }
    }
}