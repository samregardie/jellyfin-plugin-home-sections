using System.Reflection;
using Jellyfin.Plugin.SamHomeSections.Data;
using Jellyfin.Plugin.SamHomeSections.HomeScreen;
using Jellyfin.Plugin.SamHomeSections.JellyfinVersionSpecific;
using Jellyfin.Plugin.SamHomeSections.Library;
using Jellyfin.Plugin.SamHomeSections.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SamHomeSections
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<CollectionManagerProxy>();
            serviceCollection.AddSingleton<HomeScreenSectionService>();
            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton<ArrApiService>(services =>
            {
                IHttpClientFactory httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
                return ActivatorUtilities.CreateInstance<ArrApiService>(services, httpClientFactory.CreateClient());
            });
            serviceCollection.AddSingleton<ImageCacheService>(services =>
            {
                IHttpClientFactory httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
                return ActivatorUtilities.CreateInstance<ImageCacheService>(services, httpClientFactory.CreateClient());
            });
            serviceCollection.AddSingleton<UserSectionsDataCache>();
            serviceCollection.AddSingleton<PerUserComputedStatsCache>();
            serviceCollection.AddSingleton<ITranslationManager, TranslationManager>();
            serviceCollection.AddSingleton<IHomeScreenManager, HomeScreenManager>(services =>
            {
                IApplicationPaths appPaths = services.GetRequiredService<IApplicationPaths>();
                
                HomeScreenManager homeScreenManager = ActivatorUtilities.CreateInstance<HomeScreenManager>(services);
                
                string pluginLocation = Path.Combine(appPaths.PluginConfigurationsPath, typeof(HomeScreenSectionsPlugin).Namespace!);

                DirectoryInfo pluginDir = new DirectoryInfo(pluginLocation);
                pluginDir.Create();
                
                string[] extraDlls = Directory.GetFiles(pluginLocation, "*.dll", SearchOption.AllDirectories).ToArray();

                foreach (string extraDll in extraDlls)
                {
                    Assembly extraPluginAssembly = Assembly.LoadFile(extraDll);

                    Type[] homeScreenSectionTypes = extraPluginAssembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IHomeScreenSection))).ToArray();

                    foreach (Type homeScreenSectionType in homeScreenSectionTypes)
                    {
                        homeScreenManager.RegisterResultsDelegate(homeScreenSectionType);
                    }
                }

                return homeScreenManager;
            });
        }
    }
}
