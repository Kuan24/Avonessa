using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AvonessaMVC5
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.aspx/{*pathInfo}");
            routes.IgnoreRoute("{*x}", new { x = @".*\.asmx(/.*)?" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}/{p_id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional,
                    p_id = UrlParameter.Optional
                }
            );
            //routes.MapRoute(
            //    name: "Default2",
            //    url: "{controller}/{action}/{sb_id}/{p_id}",
            //    defaults: new
            //    {
            //        controller = "ShopBagOrder",
            //        action = "ShoppingBag",
            //        sb_id = UrlParameter.Optional,
            //        p_id = UrlParameter.Optional
            //    }
            //);
        }
    }
}
