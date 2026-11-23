using System.Linq;
using System.Threading.Tasks;
using Nop.Services.Plugins;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.MrPoly
{
    public class MrPolyPlugin : BasePlugin, IAdminMenuPlugin
    {
        public Task ManageSiteMapAsync(AdminMenuItem rootNode)
        {
            // Create custom PolyCommerce menu item
            var menuItem = new AdminMenuItem()
            {
                SystemName = "MrPolyPlugin",
                Title = "Mr. Poly",
                IconClass = "fa fa-male",
                Url = "/Admin/MrPoly/Index",
                Visible = true,
            };

            // Add Mr Poly menu item if it does not already exist.
            if (!rootNode.ChildNodes.Any(x => string.Equals(x.SystemName, "MrPolyPlugin")))
            {
                rootNode.ChildNodes.Add(menuItem);
            }

            return Task.FromResult(0);

        }
    }
}
