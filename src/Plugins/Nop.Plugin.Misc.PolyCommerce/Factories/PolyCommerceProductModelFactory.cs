using System.ComponentModel;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.FilterLevels;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.PriceLists;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.PolyCommerce.Factories
{
    public class PolyCommerceProductModelFactory : ProductModelFactory
    {
        public PolyCommerceProductModelFactory(CatalogSettings catalogSettings,
        CurrencySettings currencySettings,
        IAddressService addressService,
        IBaseAdminModelFactory baseAdminModelFactory,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IDiscountService discountService,
        IDiscountSupportedModelFactory discountSupportedModelFactory,
        IFilterLevelValueService filterLevelValueService,
        IHttpContextAccessor httpContextAccessor,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IManufacturerService manufacturerService,
        IMeasureService measureService,
        INopFileProvider nopFileProvider,
        IOrderService orderService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IPriceListService priceListService,
        IProductAttributeFormatter productAttributeFormatter,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IProductTagService productTagService,
        IProductTemplateService productTemplateService,
        ISettingModelFactory settingModelFactory,
        ISettingService settingService,
        IShipmentService shipmentService,
        IShoppingCartService shoppingCartService,
        ISpecificationAttributeService specificationAttributeService,
        IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
        IStoreContext storeContext,
        IStoreService storeService,
        IUrlRecordService urlRecordService,
        IVideoService videoService,
        IWarehouseService warehouseService,
        IWebHelper webHelper,
        IWorkContext workContext,
        MeasureSettings measureSettings,
        MediaSettings mediaSettings,
        NopHttpClient nopHttpClient,
        TaxSettings taxSettings,
        VendorSettings vendorSettings)
        : base(catalogSettings, currencySettings, addressService, baseAdminModelFactory, categoryService, currencyService, customerService, dateTimeHelper, discountService, discountSupportedModelFactory, filterLevelValueService, httpContextAccessor, localizationService, localizedModelFactory, manufacturerService, measureService, nopFileProvider, orderService, pictureService, priceFormatter, priceListService, productAttributeFormatter, productAttributeParser, productAttributeService, productService, productTagService, productTemplateService, settingModelFactory, settingService, shipmentService, shoppingCartService, specificationAttributeService, storeMappingSupportedModelFactory, storeContext, storeService, urlRecordService, videoService, warehouseService, webHelper, workContext, measureSettings, mediaSettings, nopHttpClient, taxSettings, vendorSettings)
    {
    }

        public override async Task<ProductModel> PrepareProductModelAsync(ProductModel model, Product product, bool excludeProperties = false)
        {
            var productModel = await base.PrepareProductModelAsync(model, product, excludeProperties);

            if (product == null)
            {
                productModel.StockQuantity = 1;
                productModel.ManageInventoryMethodId = (int)ManageInventoryMethod.ManageStock;
                productModel.MinStockQuantity = 1;
                productModel.LowStockActivityId = (int)LowStockActivity.Unpublish;
                productModel.DisplayStockQuantity = true;
                productModel.DisplayStockAvailability = true;
            }

            return productModel;
        }
    }
}
