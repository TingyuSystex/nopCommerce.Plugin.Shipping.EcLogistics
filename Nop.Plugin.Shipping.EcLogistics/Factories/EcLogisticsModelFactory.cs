using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.SqlQuery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Plugin.Shipping.EcLogistics.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Shipping.EcLogistics.Factories
{
    /// <summary>
    /// Represents plugin models factory
    /// </summary>
    public class EcLogisticsModelFactory
    {
        #region Fields

        private readonly EcLogisticsService _ecLogisticsService;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly IShippingPluginManager _shippingPluginManager;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IWorkContext _workContext;
        private readonly IShipmentService _shipmentService;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentItem> _siRepository;
        private readonly IRepository<ReturnRequest> _returnRequestRepository;
        private readonly IOrderService _orderService;
        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IAddressService _addressService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IDownloadService _downloadService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IReturnRequestModelFactory _returnRequestModelFactory;
        private readonly IReturnRequestService _returnRequestService;

        #endregion

        #region Ctor

        public EcLogisticsModelFactory(EcLogisticsService ecLogisticsService, 
            IBaseAdminModelFactory baseAdminModelFactory,
            IShippingPluginManager shippingPluginManager,
            IDateTimeHelper dateTimeHelper,
            IWorkContext workContext,
            IShipmentService shipmentService,
            IRepository<Address> addressRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Product> productRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> siRepository,
            IRepository<ReturnRequest> returnRequestRepository,
            IOrderService orderService,
            IOrderModelFactory orderModelFactory,
            IAddressModelFactory addressModelFactory,
            IAddressService addressService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IMeasureService measureService,
            MeasureSettings measureSettings,
            IPriceFormatter priceFormatter,
            IDownloadService downloadService,
            ICustomerService customerService,
            IProductService productService,
            IReturnRequestModelFactory requestModelFactory,
            IReturnRequestService returnRequestService
        )
        {
            _ecLogisticsService = ecLogisticsService;
            _baseAdminModelFactory = baseAdminModelFactory;
            _shippingPluginManager = shippingPluginManager;
            _dateTimeHelper = dateTimeHelper;
            _workContext = workContext;
            _shipmentService = shipmentService;
            _addressRepository = addressRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _productRepository = productRepository;
            _shipmentRepository = shipmentRepository;
            _siRepository = siRepository;
            _returnRequestRepository = returnRequestRepository;
            _orderService = orderService;
            _orderModelFactory = orderModelFactory;
            _addressModelFactory = addressModelFactory;
            _addressService = addressService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _measureService = measureService;
            _measureSettings = measureSettings;
            _priceFormatter = priceFormatter;
            _downloadService = downloadService;
            _customerService = customerService;
            _productService = productService;
            _returnRequestModelFactory = requestModelFactory;
            _returnRequestService = returnRequestService;
        }

        #endregion

        #region Utilities

        public async Task<IPagedList<Shipment>> GetShipmentList(bool isCreate,
            int vendorId = 0, int warehouseId = 0,
            int shippingCountryId = 0,
            int shippingStateId = 0,
            string shippingCounty = null,
            string shippingCity = null,
            string shippingMethod = "All",
            int orderId = 0,
            string orderCustomNumber = "",
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var shipments = await _shipmentRepository.GetAllPagedAsync(query =>
            {
                if (isCreate)
                    query = query.Where(o => o.TrackingNumber == null);

                if (orderId > 0)
                    query = query.Where(o => o.OrderId.ToString().Contains(orderId.ToString()));

                if (!orderCustomNumber.IsNullOrEmpty())
                    query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where o.CustomOrderNumber.Contains(orderCustomNumber)
                        select s;

                if (shippingCountryId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.CountryId == shippingCountryId)
                            select s;

                if (shippingStateId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.StateProvinceId == shippingStateId)
                            select s;

                if (!string.IsNullOrWhiteSpace(shippingCounty))
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.County.Contains(shippingCounty))
                            select s;

                if (!string.IsNullOrWhiteSpace(shippingCity))
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.City.Contains(shippingCity))
                            select s;

                if (createdFromUtc.HasValue)
                    query = query.Where(s => createdFromUtc.Value <= s.CreatedOnUtc);

                if (createdToUtc.HasValue)
                    query = query.Where(s => createdToUtc.Value >= s.CreatedOnUtc);

                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where !o.Deleted
                        select s;

                query = query.Distinct();

                if (vendorId > 0)
                {
                    var queryVendorOrderItems = from orderItem in _orderItemRepository.Table
                                                join p in _productRepository.Table on orderItem.ProductId equals p.Id
                                                where p.VendorId == vendorId
                                                select orderItem.Id;

                    query = from s in query
                            join si in _siRepository.Table on s.Id equals si.ShipmentId
                            where queryVendorOrderItems.Contains(si.OrderItemId)
                            select s;

                    query = query.Distinct();
                }

                if (warehouseId > 0)
                {
                    query = from s in query
                            join si in _siRepository.Table on s.Id equals si.ShipmentId
                            where si.WarehouseId == warehouseId
                            select s;

                    query = query.Distinct();
                }

                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where o.ShippingRateComputationMethodSystemName.Contains("Shipping.EcLogistics.")
                        select s;

                if (shippingMethod != "All")
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where o.ShippingRateComputationMethodSystemName == shippingMethod
                            select s;

                query = query.OrderByDescending(s => s.CreatedOnUtc);

                return query;
            }, pageIndex, pageSize);

            return shipments;
        }

        public async Task<IPagedList<ReturnRequest>> SearchReturnRequestsAsync(int storeId = 0, int customerId = 0,
            int orderItemId = 0, string customNumber = "", ReturnRequestStatus? rs = null, DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null, int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
        {
            var query = _returnRequestRepository.Table;
            if (storeId > 0)
                query = query.Where(rr => storeId == rr.StoreId);
            if (customerId > 0)
                query = query.Where(rr => customerId == rr.CustomerId);
            if (rs.HasValue)
            {
                var returnStatusId = (int)rs.Value;
                query = query.Where(rr => rr.ReturnRequestStatusId == returnStatusId);
            }

            if (orderItemId > 0)
                query = query.Where(rr => rr.OrderItemId == orderItemId);

            if (!string.IsNullOrEmpty(customNumber))
                query = query.Where(rr => rr.CustomNumber == customNumber);

            if (createdFromUtc.HasValue)
                query = query.Where(rr => createdFromUtc.Value <= rr.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(rr => createdToUtc.Value >= rr.CreatedOnUtc);

            query = from r in query
                join oi in _orderItemRepository.Table on r.OrderItemId equals oi.Id
                join o in _orderRepository.Table on oi.OrderId equals o.Id
                where o.ShippingRateComputationMethodSystemName.Contains("Shipping.EcLogistics.")
                select r;

            query = query.OrderByDescending(rr => rr.CreatedOnUtc).ThenByDescending(rr => rr.Id);

            var returnRequests = await query.ToPagedListAsync(pageIndex, pageSize, getOnlyTotalCount);

            return returnRequests;
        }


        #endregion

        #region Methods

        #region Shipments

        /// <summary>
        /// Prepare shipment search model
        /// </summary>
        /// <param name="searchModel">Shipment search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment search model
        /// </returns>
        public virtual async Task<EclShipmentSearchModel> PrepareEclShipmentSearchModelAsync(EclShipmentSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare available countries
            await _baseAdminModelFactory.PrepareCountriesAsync(searchModel.AvailableCountries);

            //prepare available states and provinces
            await _baseAdminModelFactory.PrepareStatesAndProvincesAsync(searchModel.AvailableStates, searchModel.CountryId);

            //prepare available warehouses
            await _baseAdminModelFactory.PrepareWarehousesAsync(searchModel.AvailableWarehouses);

            //prepare nested search model
            PrepareShipmentItemSearchModel(searchModel.ShipmentItemSearchModel);

            //prepare active EcLogistics shipping methods
            var activeShippingMethods = await _shippingPluginManager.LoadActivePluginsAsync();
            foreach (var asm in activeShippingMethods)
            {
                if(asm.PluginDescriptor.SystemName.Contains("Shipping.EcLogistics."))
                    searchModel.EclShippingMethods.Add(new SelectListItem { Value = asm.PluginDescriptor.SystemName, Text = asm.PluginDescriptor.FriendlyName});
            }
            //insert this default item at first
            searchModel.EclShippingMethods.Insert(0, new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "All" });

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        protected ShipmentItemSearchModel PrepareShipmentItemSearchModel(ShipmentItemSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        public virtual async Task<ShipmentListModel> PrepareCreateListModelAsync(EclShipmentSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get parameters to filter shipments
            var vendor = await _workContext.GetCurrentVendorAsync();
            var vendorId = vendor?.Id ?? 0;
            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
            //var orderId = searchModel.OrderNumber.IsNullOrEmpty() ? 0 : int.Parse(searchModel.OrderNumber);
            int orderId = 0;
            var notUsingCustomOrderNumber = int.TryParse(searchModel.OrderNumber, out orderId);
            if (notUsingCustomOrderNumber)
            {
                searchModel.OrderNumber = "";
            }

            var shipments = await GetShipmentList(true,
                vendorId,
                searchModel.WarehouseId,
                searchModel.CountryId,
                searchModel.StateProvinceId,
                searchModel.County,
                searchModel.City,
                searchModel.ShippingMethod,
                orderId,
                searchModel.OrderNumber,
                startDateValue,
                endDateValue,
                searchModel.Page - 1,
                searchModel.PageSize);
            
            //prepare list model
            var model = await new ShipmentListModel().PrepareToGridAsync(searchModel, shipments, () =>
            {
                //fill in model values from the entity
                return shipments.SelectAwait(async shipment => await PrepareShipmentModelAsync(shipment));
            });

            //var result = new EcLogisticsShipmentListModel();

            //foreach (var shipment in model.Data)
            //{
            //    var order = await _orderRepository.GetAllAsync(o => o.Where(o => o.Id == shipment.OrderId));
            //    var shippingMethod = order.FirstOrDefault().ShippingMethod;
            //    var newmodel = new EcLogisticsShipmentModel()
            //    {
            //        ShippingMethod = shippingMethod,
            //        Id = shipment.Id,
            //        OrderId = shipment.OrderId,
            //        CustomOrderNumber = shipment.CustomOrderNumber,
            //        TotalWeight = shipment.TotalWeight,
            //        TrackingNumber = shipment.TrackingNumber,
            //        AdminComment = shipment.AdminComment,
            //        Items = shipment.Items
            //    };
            //    result.Items.Append(newmodel);
            //}

            return model;
        }

        public virtual async Task<ShipmentListModel> PreparePrintListModelAsync(EclShipmentSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get parameters to filter shipments
            var vendor = await _workContext.GetCurrentVendorAsync();
            var vendorId = vendor?.Id ?? 0;
            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
            int orderId = 0;
            var notUsingCustomOrderNumber = int.TryParse(searchModel.OrderNumber, out orderId);
            if (notUsingCustomOrderNumber)
            {
                searchModel.OrderNumber = "";
            }

            var shipments = await GetShipmentList(false,
                vendorId,
                searchModel.WarehouseId,
                searchModel.CountryId,
                searchModel.StateProvinceId,
                searchModel.County,
                searchModel.City,
                searchModel.ShippingMethod,
                orderId,
                searchModel.OrderNumber,
                startDateValue,
                endDateValue,
                searchModel.Page - 1,
                searchModel.PageSize);

            //prepare list model
            var model = await new ShipmentListModel().PrepareToGridAsync(searchModel, shipments, () =>
            {
                //fill in model values from the entity
                return shipments.SelectAwait(async shipment => await PrepareShipmentModelAsync(shipment));
            });
            
            return model;
        }

        protected virtual async Task<ShipmentModel> PrepareShipmentModelAsync(Shipment shipment, ShipmentModel model = null)
        {
            //fill in model values from the entity
            var shipmentModel = model ?? shipment.ToModel<ShipmentModel>();

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);

            #region 註解
            //var shippingMethod = order.ShippingMethod;
            //var shipmentModel = new EcLogisticsShipmentModel()
            //{
            //    ShippingMethod = shippingMethod,
            //    Id = sm.Id,
            //    OrderId = sm.OrderId,
            //    CustomOrderNumber = sm.CustomOrderNumber,
            //    TotalWeight = sm.TotalWeight,
            //    TrackingNumber = sm.TrackingNumber,
            //    AdminComment = sm.AdminComment,
            //    Items = sm.Items
            //};
            #endregion

            shipmentModel.PickupInStore = order.PickupInStore;
            shipmentModel.CustomOrderNumber = order.CustomOrderNumber;

            //convert dates to the user time
            if (order.PickupInStore)
            {
                shipmentModel.ShippedDate = await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DateNotAvailable");
                shipmentModel.ReadyForPickupDate = shipment.ReadyForPickupDateUtc.HasValue
                    ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ReadyForPickupDateUtc.Value, DateTimeKind.Utc)).ToString()
                    : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ReadyForPickupDate.NotYet");
            }
            else
            {
                shipmentModel.ReadyForPickupDate = await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DateNotAvailable");
                shipmentModel.ShippedDate = shipment.ShippedDateUtc.HasValue
                    ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)).ToString()
                    : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ShippedDate.NotYet");
            }

            shipmentModel.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DeliveryDate.NotYet");

            //fill in additional values (not existing in the entity)
            shipmentModel.CanShip = !order.PickupInStore && !shipment.ShippedDateUtc.HasValue;
            shipmentModel.CanMarkAsReadyForPickup = order.PickupInStore && !shipment.ReadyForPickupDateUtc.HasValue;
            shipmentModel.CanDeliver = (shipment.ShippedDateUtc.HasValue || shipment.ReadyForPickupDateUtc.HasValue) && !shipment.DeliveryDateUtc.HasValue;

            if (shipment.TotalWeight.HasValue)
                shipmentModel.TotalWeight = $"{shipment.TotalWeight:F2} [{(await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name}]";

            return shipmentModel;
        }

        #endregion

        #region Return Requests

        public virtual async Task<ReturnRequestSearchModel> PrepareReturnRequestSearchModelAsync(ReturnRequestSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare available return request statuses
            await _baseAdminModelFactory.PrepareReturnRequestStatusesAsync(searchModel.ReturnRequestStatusList, false);

            searchModel.ReturnRequestStatusId = 0; //pending

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        public async Task<ReturnRequestListModel> PrepareReturnRequestListModelAsync(ReturnRequestSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
            var returnRequestStatus = ReturnRequestStatus.Pending;

            //get return requests
            var returnRequests = await SearchReturnRequestsAsync(customNumber: searchModel.CustomNumber,
                rs: returnRequestStatus,
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = await new ReturnRequestListModel().PrepareToGridAsync(searchModel, returnRequests, () =>
            {
                return returnRequests.SelectAwait(async returnRequest => await _returnRequestModelFactory.PrepareReturnRequestModelAsync(null, returnRequest));
            });

            return model;
        }

        public async Task<ReturnRequestListModel> PrepareReturnItemListModelAsync(ReturnRequestSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
            var returnRequestStatus = ReturnRequestStatus.ReturnAuthorized;

            var returnRequests = await _returnRequestService.SearchReturnItemsAsync(
               createdFromUtc: startDateValue,
               createdToUtc: endDateValue,
               logisticNumber: searchModel.LogisticNumber, 
               customNumber: searchModel.CustomNumber,
               rs: returnRequestStatus, 
               pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = await new ReturnRequestListModel().PrepareToGridAsync(searchModel, returnRequests, () =>
            {
                return returnRequests.SelectAwait(async returnRequest => await _returnRequestModelFactory.PrepareReturnRequestModelAsync(null, returnRequest));
            });

            return model;
        }


        public async Task<ReturnRequestListModel> PrepareRefundListModelAsync(ReturnRequestSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
            var returnRequestStatus = ReturnRequestStatus.ItemReceived;

            var returnRequests = await _returnRequestService.SearchRefundAsync(
                customNumber: searchModel.CustomNumber,
                orderItemId: Convert.ToInt32(searchModel.CustomOrderNumber),
                rs: returnRequestStatus,
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = await new ReturnRequestListModel().PrepareToGridAsync(searchModel, returnRequests, () =>
            {
                return returnRequests.SelectAwait(async returnRequest => await _returnRequestModelFactory.PrepareReturnRequestModelAsync(null, returnRequest));
            });

            return model;
        }

        #endregion

        #endregion
    }
}