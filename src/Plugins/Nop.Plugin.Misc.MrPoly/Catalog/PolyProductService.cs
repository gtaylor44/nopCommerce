using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using System;
using Nop.Core.Caching;
using Nop.Core.Domain.ArtificialIntelligence;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Infrastructure;
using Nop.Core;
using Nop.Data;
using Nop.Services.ArtificialIntelligence;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Vendors;

namespace Nop.Plugin.Misc.MrPoly.Catalog
{
    public class PolyProductService : ProductService
    {
        public PolyProductService(ArtificialIntelligenceSettings artificialIntelligenceSettings,
        CatalogSettings catalogSettings,
        IAclService aclService,
        IAiRecommendationPluginManager aiRecommendationPluginManager,
        ICustomerService customerService,
        IDateRangeService dateRangeService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IMeasureService measureService,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IRepository<Category> categoryRepository,
        IRepository<CrossSellProduct> crossSellProductRepository,
        IRepository<DiscountProductMapping> discountProductMappingRepository,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<Product> productRepository,
        IRepository<Product3dObject> product3dObjectRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductPicture> productPictureRepository,
        IRepository<ProductProductTagMapping> productTagMappingRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IRepository<ProductTag> productTagRepository,
        IRepository<ProductVideo> productVideoRepository,
        IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
        IRepository<RelatedProduct> relatedProductRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
        IRepository<TierPrice> tierPriceRepository,
        ISearchPluginManager searchPluginManager,
        IStaticCacheManager staticCacheManager,
        IVendorService vendorService,
        IStoreMappingService storeMappingService,
        IWorkContext workContext,
        LocalizationSettings localizationSettings)
        : base(artificialIntelligenceSettings, catalogSettings, aclService, aiRecommendationPluginManager, customerService, dateRangeService, languageService, localizationService, measureService, productAttributeParser, productAttributeService, categoryRepository, crossSellProductRepository, discountProductMappingRepository, localizedPropertyRepository, manufacturerRepository, productRepository, product3dObjectRepository, productAttributeCombinationRepository, productAttributeMappingRepository, productCategoryRepository, productManufacturerRepository, productPictureRepository, productTagMappingRepository, productSpecificationAttributeRepository, productTagRepository, productVideoRepository, productWarehouseInventoryRepository, relatedProductRepository, shipmentRepository, stockQuantityHistoryRepository, tierPriceRepository, searchPluginManager, staticCacheManager, vendorService, storeMappingService, workContext, localizationSettings)
    {
    }

        public override async Task<IList<Product>> GetAllProductsDisplayedOnHomepageAsync()
        {
            var query = from p in _productRepository.Table
                        orderby p.CreatedOnUtc descending
                        where p.Published &&
                        !p.Deleted
                        && p.CreatedOnUtc >= DateTime.UtcNow.AddDays(-14)
                        select p;
            var products = await query.ToListAsync();
            return products;
        }
    }
}
