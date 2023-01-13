using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Shipping.EcLogistics.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {

            endpointRouteBuilder.MapControllerRoute("Plugin.Shipping.EcLogistics.StatusReply", "Plugins/EcLogistics/StatusReply",
                 new { controller = "EcLogistics", action = "StatusReply" });

            endpointRouteBuilder.MapControllerRoute("Plugin.Shipping.EcLogistics.ReturnStatusReply", "Plugins/EcLogistics/ReturnStatusReply",
                 new { controller = "EcLogistics", action = "ReturnStatusReply" });

        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;
    }
}