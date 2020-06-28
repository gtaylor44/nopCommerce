﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.PolyCommerce.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.PolyCommerce.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IRepository<Language> _languageRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<OrderNote> _orderNoteRepository;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICountryService _countryService;
        private readonly ILogger _logger;
        private readonly ICustomNumberFormatter _customNumberFormatter;
        private readonly IEventPublisher _eventPublisher;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IProductAttributeService _productAttributeService;

        public OrdersController(IRepository<Order> orderRepository,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            ICustomerService customerService,
            IRepository<Language> languageRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<OrderNote> orderNoteRepository,
            IGenericAttributeService genericAttributeService,
            ILogger logger,
            IEventPublisher eventPublisher,
            ICustomNumberFormatter customNumberFormatter,
            ICountryService countryService,
            ICurrencyService currencyService,
            IOrderService orderService,
            CurrencySettings currencySettings,
            IProductService productService, 
            IProductAttributeService productAttributeService)
        {
            _orderRepository = orderRepository;
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _customerService = customerService;
            _languageRepository = languageRepository;
            _orderItemRepository = orderItemRepository;
            _orderNoteRepository = orderNoteRepository;
            _genericAttributeService = genericAttributeService;
            _countryService = countryService;
            _logger = logger;
            _orderService = orderService;
            _customNumberFormatter = customNumberFormatter;
            _eventPublisher = eventPublisher;
            _productService = productService;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _productAttributeService = productAttributeService;
        }

        [HttpGet]
        [Route("api/polycommerce/orders/get_store_currency")]
        public async Task<IActionResult> GetStoreCurrency()
        {
            try
            {
                var storeToken = Request.Headers.TryGetValue("Store-Token", out var values) ? values.First() : null;
                var store = await PolyCommerceHelper.GetPolyCommerceStoreByToken(storeToken);

                if (store == null)
                {
                    return Unauthorized();
                }

                var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

                return Ok(new { CurrencyCode = primaryStoreCurrency.CurrencyCode });
                
            }
            catch(Exception ex)
            {
                _logger.Error("Error while fetching Store Currency", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/polycommerce/orders/add")]
        public async Task<IActionResult> AddOrder([FromBody]PolyCommerceOrder model)
        {
            Order order = null;
            try
            {
                var storeToken = Request.Headers.TryGetValue("Store-Token", out var values) ? values.First() : null;
                var store = await PolyCommerceHelper.GetPolyCommerceStoreByToken(storeToken);

                if (store == null)
                {
                    return Unauthorized();
                }

                var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

                if (model == null)
                {
                    throw new Exception("Model can't be null");
                }

                if (model.Address == null)
                {
                    throw new Exception("Address can't be null");
                }

                if (model.OrderItems == null || !model.OrderItems.Any())
                {
                    throw new Exception("At least one OrderItem is required");
                }

                if (!Enum.IsDefined(typeof(PaymentStatus), model.PaymentStatusId))
                {
                    throw new Exception($"PaymentStatusId: {model.PaymentStatusId} not recognised");
                }

                if (!Enum.IsDefined(typeof(OrderStatus), model.OrderStatusId))
                {
                    throw new Exception($"OrderStatusId: {model.OrderStatusId} not recognised");
                }

                var country = _countryService.GetCountryByTwoLetterIsoCode(model.Address.TwoLetterCountryCode);

                // only english supported
                var englishLanguage = _languageRepository.Table.First(x => x.Name == "English");

                var customer = new Customer
                {
                    Username = model.Email,
                    Email = model.Email,
                    Active = true,
                    Deleted = false,
                    LastActivityDateUtc = DateTime.UtcNow,
                    CreatedOnUtc = DateTime.UtcNow,
                    IsSystemAccount = false,
                    ShippingAddress = new Address
                    {
                        FirstName = model.Address.FirstName,
                        LastName = model.Address.LastName,
                        Address1 = model.Address.Address1,
                        Address2 = model.Address.Address2,
                        City = model.Address.City,
                        PhoneNumber = model.Address.PhoneNumber,
                        Email = model.Email,
                        CountryId = country.Id,
                        Company = model.Address.Company,
                        ZipPostalCode = model.Address.ZipPostalCode
                    }
                };

                // add new customer to system
                _customerService.InsertCustomer(customer);

                // assign guest role to newly added customer
                var guestRole = _customerService.GetCustomerRoleBySystemName(NopCustomerDefaults.GuestsRoleName);
                customer.AddCustomerRoleMapping(new CustomerCustomerRoleMapping { CustomerRole = guestRole });

                try
                {
                    // save FirstName and LastName to user.
                    if (!string.IsNullOrEmpty(model.Address.FirstName))
                    {
                        _genericAttributeService.SaveAttribute(customer, "FirstName", model.Address.FirstName);
                    }

                    if (!string.IsNullOrEmpty(model.Address.LastName))
                    {
                        _genericAttributeService.SaveAttribute(customer, "LastName", model.Address.LastName);
                    }
                }
                catch(Exception ex)
                {
                    _logger.Warning("Could not save generic attributes: FirstName, LastName", ex);
                }

                order = new Order
                {
                    StoreId = store.Id,
                    OrderGuid = Guid.NewGuid(),
                    Customer = customer,
                    CustomerLanguageId = englishLanguage.Id,
                    CustomerIp = null,
                    OrderSubtotalInclTax = model.OrderSubtotalInclTax,
                    OrderSubtotalExclTax = model.OrderSubtotalExclTax,
                    OrderSubTotalDiscountInclTax = decimal.Zero,
                    OrderSubTotalDiscountExclTax = decimal.Zero,
                    PaymentMethodAdditionalFeeInclTax = decimal.Zero,
                    PaymentMethodAdditionalFeeExclTax = decimal.Zero,
                    TaxRates = "0:0;",
                    OrderTax = decimal.Zero,
                    OrderTotal = model.OrderTotal,
                    RefundedAmount = decimal.Zero,
                    OrderDiscount = decimal.Zero,
                    CheckoutAttributeDescription = string.Empty,
                    CheckoutAttributesXml = string.Empty,
                    CustomerCurrencyCode = primaryStoreCurrency.CurrencyCode,
                    AffiliateId = 0,
                    OrderStatus = (OrderStatus)model.OrderStatusId,
                    AllowStoringCreditCardNumber = false,
                    CardType = string.Empty,
                    CardName = string.Empty,
                    CardNumber = string.Empty,
                    MaskedCreditCardNumber = string.Empty,
                    CardCvv2 = string.Empty,
                    CardExpirationMonth = string.Empty,
                    CardExpirationYear = string.Empty,
                    PaymentMethodSystemName = model.PaymentMethodName,
                    AuthorizationTransactionId = string.Empty,
                    AuthorizationTransactionCode = string.Empty,
                    AuthorizationTransactionResult = string.Empty,
                    CaptureTransactionId = string.Empty,
                    CaptureTransactionResult = string.Empty,
                    SubscriptionTransactionId = string.Empty,
                    PaymentStatus = (PaymentStatus)model.PaymentStatusId,
                    PaidDateUtc = null,
                    BillingAddress = (Address)customer.ShippingAddress.Clone(),
                    ShippingAddress = (Address)customer.ShippingAddress.Clone(),
                    ShippingStatus = ShippingStatus.NotYetShipped,
                    ShippingMethod = model.ShippingMethod,
                    PickupInStore = false,
                    CustomValuesXml = string.Empty,
                    VatNumber = string.Empty,
                    CreatedOnUtc = DateTime.UtcNow,
                    CurrencyRate = 1,
                    CustomOrderNumber = string.Empty,
                    OrderShippingInclTax = model.OrderShippingTotalInclTax,
                    OrderShippingExclTax = model.OrderShippingTotalExclTax
                };

                _orderService.InsertOrder(order);

                //generate and set custom order number
                order.CustomOrderNumber = _customNumberFormatter.GenerateOrderCustomNumber(order);
                _orderService.UpdateOrder(order);

                // insert order items...
                foreach (var orderItem in model.OrderItems)
                {
                    var product = _productService.GetProductById((int)orderItem.ExternalProductId);

                    var newItem = new OrderItem
                    {
                        OrderItemGuid = Guid.NewGuid(),
                        Order = order,
                        ProductId = product.Id,
                        UnitPriceInclTax = orderItem.UnitPriceInclTax,
                        UnitPriceExclTax = orderItem.UnitPriceExclTax,
                        PriceInclTax = orderItem.PriceInclTax,
                        PriceExclTax = orderItem.PriceExclTax,
                        AttributeDescription = string.Empty,
                        AttributesXml = string.Empty,
                        Quantity = orderItem.Quantity,
                        DiscountAmountInclTax = decimal.Zero,
                        DiscountAmountExclTax = decimal.Zero,
                        DownloadCount = 0,
                        IsDownloadActivated = false,
                        LicenseDownloadId = null,
                        ItemWeight = orderItem.ItemWeight
                    };

                    _orderItemRepository.Insert(newItem);
                    _productService.AdjustInventory(product, orderItem.Quantity * -1);

                    // NopCommerce Core does not update UpdatedOnUtc date inside AdjustInventory() method
                    product.UpdatedOnUtc = DateTime.UtcNow;
                    _productService.UpdateProduct(product);
                }

                try
                {
                    //order notes
                    if (model.Notes != null && model.Notes.Any())
                    {
                        foreach (var note in model.Notes)
                        {
                            _orderNoteRepository.Insert(new OrderNote
                            {
                                CreatedOnUtc = DateTime.UtcNow,
                                Note = note,
                                Order = order
                            });
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.Warning("Could not save order notes", ex);
                }

                try
                {
                    _customerActivityService.InsertActivity("PublicStore.PlaceOrder", string.Format(_localizationService.GetResource("ActivityLog.PublicStore.PlaceOrder"), order.Id), order);
                    _eventPublisher.Publish(new OrderPlacedEvent(order));
                }
                catch(Exception ex)
                {
                    _logger.Warning("Could not publish order events", ex);
                }

                return Ok(new { OrderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving PolyCommerce order. {(model != null ? Environment.NewLine + JsonConvert.SerializeObject(model) : string.Empty)}", ex);

                if (order != null && order.Id > 0)
                {
                    _orderService.DeleteOrder(order);
                }

                return BadRequest(ex.ToString());
            }
        }

        [HttpPost]
        [Route("api/polycommerce/orders/check_for_shipped_orders")]
        public async Task<IActionResult> CheckForShippedOrders([FromBody]PolyCommerceCheckForShippedOrdersModel model)
        {
            var storeToken = Request.Headers.TryGetValue("Store-Token", out var values) ? values.First() : null;

            var store = await PolyCommerceHelper.GetPolyCommerceStoreByToken(storeToken);

            if (store == null)
            {
                return Unauthorized();
            }

            if (model == null)
            {
                throw new Exception("Model can't be null");
            }

            if (model.OrderIds == null || !model.OrderIds.Any())
            {
                throw new Exception("Expected at least one OrderIds element.");
            }

            var orders = await _orderRepository.Table
                .Where(x => model.OrderIds.Any(y => y == x.Id) && x.ShippingStatusId > (int)ShippingStatus.NotYetShipped)
                .Select(x => new PolyCommerceCheckForShippedOrdersResult { OrderId = x.Id, Shipped = true })
                .ToListAsync();

            return Ok(orders);
        }

    }
}
