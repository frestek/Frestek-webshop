using DotNetNuke.Web.Api;
using System.Web.Http;

namespace Frestek.Vision.Modul
{
    public class RouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute(
                moduleFolderName: "FrestekVision",
                routeName: "default",
                url: "{controller}/{action}",
                namespaces: new[] { "Frestek.Vision.Modul.Services" }
            );
        }
    }
}