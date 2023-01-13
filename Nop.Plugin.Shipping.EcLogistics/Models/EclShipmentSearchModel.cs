using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    /// <summary>
    /// Represents a shipment search model
    /// </summary>
    public partial record EclShipmentSearchModel : BaseSearchModel
    {
        #region Ctor

        public EclShipmentSearchModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            EclShippingMethods = new List<SelectListItem>();
            ShipmentItemSearchModel = new ShipmentItemSearchModel();
            AvailableTemperatureType = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Admin.Catalog.Products.Fields.AvailableProductTemperatureType")]
        public int TemperatureTypeId { get; set; }
        public IList<SelectListItem> AvailableTemperatureType { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.CustomOrderNumber")]
        public string OrderNumber { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.Country")]
        public int CountryId { get; set; }

        public IList<SelectListItem> AvailableStates { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.StateProvince")]
        public int StateProvinceId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.County")]
        public string County { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.City")]
        public string City { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.Warehouse")]
        public int WarehouseId { get; set; }

        public IList<SelectListItem> AvailableWarehouses { get; set; }

        public ShipmentItemSearchModel ShipmentItemSearchModel { get; set; }

        [NopResourceDisplayName("Admin.Orders.Fields.ShippingMethod")]
        public string ShippingMethod { get; set; }

        public IList<SelectListItem> EclShippingMethods { get; set; }

        public bool isInit { get; set; }
        #endregion
    }
}