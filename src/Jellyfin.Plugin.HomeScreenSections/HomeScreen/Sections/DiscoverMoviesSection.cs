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
    public class DiscoverMoviesSection : DiscoverSection
    {
        public override string? Section => "DiscoverMovies";

        public override string? DisplayText { get; set; } = "Discover Movies";

        protected override string JellyseerEndpoint => "/api/v1/discover/movies";

        public DiscoverMoviesSection(IUserManager userManager, ImageCacheService imageCacheService) 
            : base(userManager, imageCacheService)
        {
        }
    }
}