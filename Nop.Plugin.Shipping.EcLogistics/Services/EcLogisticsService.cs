using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LinqToDB.Common;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.EcLogistics.Services
{
    public class EcLogisticsService
    {
        #region Fields

        private readonly EcLogisticsSettings _ecLogisticsSettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly ShippingSettings _shippingSettings;
        private readonly ISettingService _settingService;
        private readonly IAddressService _addressService;
        private readonly IRepository<EcPayHomeShippingMethod> _ecPayHomeShippingMethodRepository;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly ILocalizationService _localizationService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IStateProvinceService _stateProvinceService;

        #endregion

        #region Ctor

        public EcLogisticsService(HttpClient client,
            EcLogisticsSettings ecLogisticsSettings,
            IWebHelper webHelper,
            IOrderService orderService,
            IShipmentService shipmentService,
            ShippingSettings shippingSettings,
            ISettingService settingService,
            IAddressService addressService,
            IRepository<EcPayHomeShippingMethod> ecPayHomeShippingMethodRepository,
            IStoreContext storeContext,
            IWorkContext workContext,
            IMessageTemplateService messageTemplateService,
            ILocalizationService localizationService,
            IEmailAccountService emailAccountService,
            IMessageTokenProvider messageTokenProvider,
            EmailAccountSettings emailAccountSettings,
            IRepository<EmailAccount> emailAccountRepository,
            IEventPublisher eventPublisher,
            IStateProvinceService stateProvinceService,
            IWorkflowMessageService workflowMessageService
        )
        {
            //configure client
            client.Timeout = TimeSpan.FromSeconds(25);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"nopCommerce-{NopVersion.CURRENT_VERSION}");

            _ecLogisticsSettings = ecLogisticsSettings;
            _webHelper = webHelper;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _shippingSettings = shippingSettings;
            _settingService = settingService;
            _addressService = addressService;
            _ecPayHomeShippingMethodRepository = ecPayHomeShippingMethodRepository;
            _storeContext = storeContext;
            _workContext = workContext;
            _messageTemplateService = messageTemplateService;
            _localizationService = localizationService;
            _emailAccountService = emailAccountService;
            _messageTokenProvider = messageTokenProvider;
            _emailAccountSettings = emailAccountSettings;
            _emailAccountRepository = emailAccountRepository;
            _eventPublisher = eventPublisher;
            _workflowMessageService = workflowMessageService;
            _stateProvinceService = stateProvinceService;
        }

        #endregion

        #region Utilities

        public static string generateCheckMacValue(string QueryString, string HashKey, string HashIV)
        {
            string strRaw = "HashKey=" + HashKey + "&" + QueryString + "&HashIV=" + HashIV;
            var encodedRaw = System.Web.HttpUtility.UrlEncode(strRaw).ToLower();
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, encodedRaw);

                Console.WriteLine("The MD5 hash of " + encodedRaw + " is: " + hash + ".");
                Console.WriteLine("Verifying the hash...");

                if (VerifyMd5Hash(md5Hash, encodedRaw, hash))
                {
                    Console.WriteLine("The hashes are the same.");
                    return hash.ToUpper();
                }
                Console.WriteLine("The hashes are not same.");
            }
            throw new Exception("The hashes are not same or fail to generate.");
        }

        private static string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        private static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return (0 == comparer.Compare(hashOfInput, hash));
        }

        private static FormUrlEncodedContent attachCheckMacValue(Dictionary<string, string> parameters, string HashKey, string HashIV)
        {
            string strQuery = "";
            //Generate QueryString

            strQuery = "HashKey=" + HashKey + "&" + ToQueryString(parameters) + "&HashIV=" + HashIV;
            var encodedRaw = System.Web.HttpUtility.UrlEncode(strQuery).ToLower();
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, encodedRaw).ToUpper();

                Console.WriteLine("The MD5 hash of " + encodedRaw + " is: " + hash + ".");
                Console.WriteLine("Verifying the hash...");

                if (VerifyMd5Hash(md5Hash, encodedRaw, hash))
                {
                    Console.WriteLine("The hashes are the same.");
                    parameters.Add("CheckMacValue", hash);
                    return new FormUrlEncodedContent(parameters);
                }
                throw new Exception("The hashes are not same.");
            }
        }

        public static string ToQueryString(IDictionary<string, string> dict)
        {
            if (dict.Count == 0) return string.Empty;

            var buffer = new StringBuilder();
            int count = 0;
            bool end = false;

            foreach (var key in dict.Keys)
            {
                if (count == dict.Count - 1) end = true;

                if (end)
                    buffer.AppendFormat("{0}={1}", key, dict[key]);
                else
                    buffer.AppendFormat("{0}={1}&", key, dict[key]);

                count++;
            }

            return buffer.ToString();
        }

        #endregion

        #region Method

        public async Task<bool> SendTestRequestAsync(string subType)
        {
            using (var httpClient = new HttpClient())
            {
                var devUrl = "https://logistics-stage.ecpay.com.tw/Express/CreateTestData";
                var prodUrl = "https://logistics.ecpay.com.tw/Express/CreateTestData";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                subType = subType.ToUpper();

                var queryStr = "LogisticsSubType=" + subType + "&MerchantID=" + merchantId;
                var cmv = generateCheckMacValue(queryStr, hashKey, hashIV);

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("LogisticsSubType", subType);
                parameters.Add("CheckMacValue", cmv);

                var httpContent = new FormUrlEncodedContent(parameters);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();
                var result = response.Substring(0, 1);

                return httpResponse.IsSuccessStatusCode && int.Parse(result) == 1;
            }
        }

        public async Task<string> CreateCvsAsync(Shipment shipment)
        {
            using (var httpClient = new HttpClient())
            {
                var storeLocation = _webHelper.GetStoreLocation();
                var devUrl = "https://logistics-stage.ecpay.com.tw/Express/Create";
                var prodUrl = "https://logistics.ecpay.com.tw/Express/Create";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                var subType = order.ShippingRateComputationMethodSystemName.Substring(21).ToUpper();
                var isCollection = order.PaymentMethodSystemName.Contains("PayInStore");
                var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
                var orderItem = await _orderService.GetOrderItemByIdAsync(shipmentItems.FirstOrDefault().OrderItemId);
                var product = await _orderService.GetProductByOrderItemIdAsync(orderItem.Id);
                var shippingOrigin = await _addressService.GetAddressByIdAsync(_shippingSettings.ShippingOriginAddressId);
                var shippingAddressId = order.ShippingAddressId.HasValue ? order.ShippingAddressId.Value : 0;
                var shippingAddress = await _addressService.GetAddressByIdAsync(shippingAddressId);
                var amount = (int)order.OrderTotal;

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("MerchantTradeNo", order.Id + "-" + shipment.Id); //TODO
                parameters.Add("MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                parameters.Add("LogisticsType", "CVS");
                parameters.Add("LogisticsSubType", subType);
                parameters.Add("GoodsAmount", amount.ToString());
                if (isCollection)
                {
                    parameters.Add("IsCollection", "Y");
                    parameters.Add("CollectionAmount", amount.ToString());
                }
                parameters.Add("GoodsName", product.Name.Substring(0, 25));
                parameters.Add("SenderName", shippingOrigin.LastName + shippingOrigin.FirstName);
                parameters.Add("ReceiverName", shippingAddress.LastName + shippingAddress.FirstName);
                parameters.Add("ReceiverCellPhone", shippingAddress.CellPhone);
                parameters.Add("ServerReplyURL", $"{storeLocation}Plugins/EcLogistics/StatusReply");
                if (!platformId.IsNullOrEmpty())
                {
                    parameters.Add("PlatformID", platformId);
                }
                parameters.Add("ReceiverStoreID", shippingAddress.CvsStoreId);

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                var isSuccess = response.Substring(0, 2) == "1|";
                if (isSuccess)
                {
                    var respBody = HttpUtility.ParseQueryString(response.Substring(2));
                    var trackingNumber = respBody["AllPayLogisticsID"];

                    return trackingNumber;
                }

                throw new NopException(response.Substring(response.IndexOf("|") + 1));
            }
        }

        public async Task<string> CreateHomeAsync(Shipment shipment)
        {
            using (var httpClient = new HttpClient())
            {
                var storeLocation = _webHelper.GetStoreLocation();
                var devUrl = "https://logistics-stage.ecpay.com.tw/Express/Create";
                var prodUrl = "https://logistics.ecpay.com.tw/Express/Create";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                var subType = order.ShippingRateComputationMethodSystemName.Substring(21).ToUpper();
                var shippingOrigin =
                    await _addressService.GetAddressByIdAsync(_shippingSettings.ShippingOriginAddressId);
                var shippingOriginCity = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingOrigin);
                var shippingAddressId = order.ShippingAddressId ?? 0;
                var shippingAddress = await _addressService.GetAddressByIdAsync(shippingAddressId);
                var shippingCity = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress);
                var amount = (int)order.OrderTotal;

                decimal totalSize = 0;
                decimal totalWeight = 0;
                var orderItems = await _orderService.GetOrderItemsAsync(shipment.OrderId);
                foreach (var item in orderItems)
                {
                    var product = await _orderService.GetProductByOrderItemIdAsync(item.Id);
                    totalSize += (product.Height + product.Length + product.Width);
                    totalWeight += product.Weight;
                }

                var temperatureId = (await _orderService.GetProductByOrderItemIdAsync(orderItems.FirstOrDefault().Id)).TemperatureTypeId;
                var temperature = "0001";
                if (temperatureId == (int)ProductTemperatureType.Low)
                    temperature = "0002";
                if (temperatureId == (int)ProductTemperatureType.Freeze)
                    temperature = "0003";

                var size = "";
                if (totalSize <= 60)
                    size = "0001";
                else if (totalSize is > 60 and <= 90)
                    size = "0002";
                else if (totalSize is > 90 and <= 120)
                    size = "0003";
                else if (totalSize is > 120 and <= 150 && temperature == "0001") 
                    size = "0004";
                else
                {
                    throw new NopException("商品長寬高總和超過150cm。冷藏或冷凍不得超過120cm。");
                }
                var pickupTime = _ecPayHomeShippingMethodRepository.GetAll().FirstOrDefault(x => x.Name == subType).ScheduledPickupTime;


                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("MerchantTradeNo", order.Id + "-" + shipment.Id); //TODO
                parameters.Add("MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                parameters.Add("LogisticsType", "HOME");
                parameters.Add("LogisticsSubType", subType);
                parameters.Add("GoodsAmount", amount.ToString());
                parameters.Add("GoodsWeight", $"{totalWeight:0.000}");
                parameters.Add("SenderName", shippingOrigin.LastName + shippingOrigin.FirstName);
                parameters.Add("SenderPhone", shippingOrigin.PhoneNumber);
                parameters.Add("SenderCellPhone", shippingOrigin.CellPhone);
                parameters.Add("SenderZipCode", shippingOrigin.ZipPostalCode);
                parameters.Add("SenderAddress", shippingOriginCity.Name + shippingOrigin.County + shippingOrigin.Address1);
                parameters.Add("ReceiverName", shippingAddress.LastName + shippingAddress.FirstName);
                parameters.Add("ReceiverCellPhone", shippingAddress.CellPhone);
                parameters.Add("ReceiverZipCode", shippingAddress.ZipPostalCode);
                parameters.Add("ReceiverAddress", shippingCity.Name + shippingAddress.County + shippingAddress.Address1); 
                if (subType == "TCAT")
                {
                    parameters.Add("Temperature", temperature); 
                    parameters.Add("Specification", size);
                    parameters.Add("ScheduledPickupTime", pickupTime.ToString());
                    parameters.Add("ScheduledDeliveryTime", "4"); //TODO
                }
                else
                {
                    parameters.Add("Temperature", "0001");
                }
                parameters.Add("ServerReplyURL", $"{storeLocation}Plugins/EcLogistics/StatusReply");
                if (!platformId.IsNullOrEmpty())
                {
                    parameters.Add("PlatformID", platformId);
                }

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                var isSuccess = response.Substring(0, 2) == "1|";
                if (isSuccess)
                {
                    var respBody = HttpUtility.ParseQueryString(response.Substring(2));
                    var trackingNumber = respBody["AllPayLogisticsID"];
                    //var bookingNote = respBody["BookingNote"]; // what's this for???

                    return trackingNumber;
                }

                throw new NopException(response.Substring(response.IndexOf("|") + 1));
            }
        }

        public async Task<string> PrintTradeDocumentAsync(string idString)
        {
            using (var httpClient = new HttpClient())
            {
                var devUrl = "https://logistics-stage.ecpay.com.tw/helper/printTradeDocument";
                var prodUrl = "https://logistics.ecpay.com.tw/helper/printTradeDocument";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("AllPayLogisticsID", idString);
                if (!platformId.IsNullOrEmpty())
                {
                    parameters.Add("PlatformID", platformId);
                }

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                return response;
            }
        }

        public async Task<string> QueryTradeInfoAsync(Shipment shipment)
        {
            using (var httpClient = new HttpClient())
            {
                var storeLocation = _webHelper.GetStoreLocation();
                var devUrl = "https://logistics-stage.ecpay.com.tw/Helper/QueryLogisticsTradeInfo/V3";
                var prodUrl = "https://logistics.ecpay.com.tw/Helper/QueryLogisticsTradeInfo/V3";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("AllPayLogisticsID", shipment.TrackingNumber);
                parameters.Add("TimeStamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                if (!platformId.IsNullOrEmpty())
                {
                    parameters.Add("PlatformID", platformId);
                }

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                var respBody = HttpUtility.ParseQueryString(response);
                var shipmentNo = respBody["ShipmentNo"];
                var bookingNote = respBody["BookingNote"];

                return shipmentNo.IsNullOrEmpty() ? bookingNote : shipmentNo; 
            }
        }

        public async Task<string> ReturnCvsAsync(List<ReturnRequest> returnRequests, string subType)
        {
            using (var httpClient = new HttpClient())
            {
                var storeLocation = _webHelper.GetStoreLocation();
                var devUrl = "https://logistics-stage.ecpay.com.tw/express/Return";
                var prodUrl = "https://logistics.ecpay.com.tw/express/Return";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;
                subType = subType == "FAMI" ? "" : subType;
                baseUri = baseUri + subType + "CVS";

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var products = returnRequests.Select(async r => 
                    await _orderService.GetProductByOrderItemIdAsync(r.OrderItemId)).Select(p => p.Result).ToList();
                var productNames = string.Join("#", products.Select(p => p.Name).Distinct().ToList());

                var order = await _orderService.GetOrderByOrderItemAsync(returnRequests.FirstOrDefault().OrderItemId);
                var trackingNum = (await _shipmentService.GetShipmentsByOrderIdAsync(order.Id)).FirstOrDefault().TrackingNumber;
                var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                var amount = (int)(from r in returnRequests
                                   join oi in orderItems
                                       on r.OrderItemId equals oi.Id
                                   join p in products
                                       on oi.ProductId equals p.Id
                                   select r.Quantity * p.Price).Sum();

                var senderAddress = await _addressService.GetAddressByIdAsync((int)order.ShippingAddressId);

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("AllPayLogisticsID", trackingNum);
                parameters.Add("ServerReplyURL", $"{storeLocation}Plugins/EcLogistics/ReturnStatusReply");
                parameters.Add("GoodsName", productNames.Substring(0, 25));
                parameters.Add("GoodsAmount", amount.ToString());
                parameters.Add("CollectionAmount", "0");
                parameters.Add("ServiceType", "4");
                parameters.Add("SenderName", senderAddress.LastName + senderAddress.FirstName);
                parameters.Add("SenderPhone", senderAddress.CellPhone);
                parameters.Add("Remark", string.Join(" ", returnRequests.Select(r => r.CustomerComments).Distinct().ToList()));
                if (!platformId.IsNullOrEmpty())
                {
                    parameters.Add("PlatformID", platformId);
                }

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                var line = response.IndexOf("|");
                if (line != 0)
                    return response.Substring(line + 1);

                throw new NopException(response.Substring(line + 1));
            }
        }

        public async Task<bool> ReturnHomeAsync(List<ReturnRequest> returnRequests)
        {
            using (var httpClient = new HttpClient())
            {
                var storeLocation = _webHelper.GetStoreLocation();
                var devUrl = "https://logistics-stage.ecpay.com.tw/express/ReturnHome";
                var prodUrl = "https://logistics.ecpay.com.tw/express/ReturnHome";
                var baseUri = _ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                var platformId = _ecLogisticsSettings.PlatformId;

                var products = returnRequests.Select(async r =>
                    await _orderService.GetProductByOrderItemIdAsync(r.OrderItemId)).Select(p => p.Result).DistinctBy(p => p.Id).ToList();
                
                var order = await _orderService.GetOrderByOrderItemAsync(returnRequests.FirstOrDefault().OrderItemId);
                var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                var amount = (int)(from r in returnRequests
                                   join oi in orderItems
                                       on r.OrderItemId equals oi.Id
                                   join p in products
                                       on oi.ProductId equals p.Id
                                   select r.Quantity * p.Price).Sum();

                var senderAddress = await _addressService.GetAddressByIdAsync((int)order.ShippingAddressId);

                var subType = order.ShippingRateComputationMethodSystemName.Substring(21).ToUpper();
                var trackingNum = (await _shipmentService.GetShipmentsByOrderIdAsync(order.Id)).FirstOrDefault().TrackingNumber;

                decimal totalSize = 0;
                foreach (var item in orderItems)
                {
                    var product = await _orderService.GetProductByOrderItemIdAsync(item.Id);
                    totalSize += (product.Height + product.Length + product.Width);
                }
                
                var temperatureId = (await _orderService.GetProductByOrderItemIdAsync(orderItems.FirstOrDefault().Id)).TemperatureTypeId;
                var temperature = "0001";
                if (temperatureId == (int)ProductTemperatureType.Low)
                    temperature = "0002";
                if (temperatureId == (int)ProductTemperatureType.Freeze)
                    temperature = "0003";

                var size = "";
                if (totalSize <= 60)
                    size = "0001";
                else if (totalSize is > 60 and <= 90)
                    size = "0002";
                else if (totalSize is > 90 and <= 120)
                    size = "0003";
                else if (totalSize is > 120 and <= 150 && temperature == "0001") 
                    size = "0004";
                else
                {
                    throw new NopException("商品長寬高總和超過150cm。冷藏或冷凍不得超過120cm。");
                }

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID", merchantId);
                parameters.Add("AllPayLogisticsID", trackingNum);
                parameters.Add("LogisticsSubType", subType);
                parameters.Add("ServerReplyURL", $"{storeLocation}Plugins/EcLogistics/ReturnStatusReply");
                parameters.Add("GoodsAmount", amount.ToString());
                if (subType == "TCAT")
                    parameters.Add("Specification", size);
                parameters.Add("Temperature", temperature);
                parameters.Add("Remark", string.Join(" ", returnRequests.Select(r => r.CustomerComments).Distinct().ToList()));
                if (!platformId.IsNullOrEmpty())
                    parameters.Add("PlatformID", platformId);

                var orderedParameters = parameters.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value);

                var httpContent = attachCheckMacValue(orderedParameters, hashKey, hashIV);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = await httpClient.PostAsync("", httpContent);
                var response = await httpResponse.Content.ReadAsStringAsync();

                if (response.Trim() == "1|OK")
                    return true;
                
                throw new NopException(response.Substring(response.IndexOf("|") + 1));
            }
        }

        public async Task<int> SendReturnRequestNumberEmailAsync(bool isCvs, Order order, string returnNumber)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

            //get message templates by the name
            var messageTemplateName = isCvs ? "ReturnRequest.Authorized.Cvs" : "ReturnRequest.Authorized.Home";
            var messageTemplates = await _messageTemplateService.GetMessageTemplatesByNameAsync(messageTemplateName, store.Id);

            //no template found
            if (!messageTemplates?.Any() ?? true)
                messageTemplates = new List<MessageTemplate>();

            //filter active templates
            messageTemplates = messageTemplates.Where(messageTemplate => messageTemplate.IsActive).ToList();

            if (!messageTemplates.Any())
                return 1;

            var messageTemplate = messageTemplates.FirstOrDefault();

            //tokens
            var commonTokens = new List<Token>
            {
                new Token("ReturnRequest.ReturnNumber", returnNumber),
                new Token("ReturnRequest.HistoryURLForCustomer", _webHelper.GetStoreLocation() + "returnrequest/history")
            };
            //order.ShippingMethod = isCvs ? order.ShippingMethod.Substring(0, order.ShippingMethod.IndexOf("超商")) : order.ShippingMethod;

            //email account
            var emailAccountId = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.EmailAccountId, languageId);
            //some 0 validation (for localizable "Email account" dropdownlist which saves 0 if "Standard" value is chosen)
            if (emailAccountId == 0)
                emailAccountId = _emailAccountRepository.Table.FirstOrDefault().Id;

            var emailAccount = (await _emailAccountService.GetEmailAccountByIdAsync(emailAccountId) ?? await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId)) ??
                               (await _emailAccountService.GetAllEmailAccountsAsync()).FirstOrDefault();

            var tokens = new List<Token>(commonTokens);
            await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount);
            await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId);

            string fromEmail = emailAccount.Email;
            string fromName = emailAccount.DisplayName;

            //event notification
            await _eventPublisher.MessageTokensAddedAsync(messageTemplate, tokens);

            var billAddress = await _addressService.GetAddressByIdAsync((int)order.BillingAddressId);
            var shippingAddress = await _addressService.GetAddressByIdAsync((int)order.ShippingAddressId);
            var toEmail = shippingAddress.Email.IsNullOrEmpty() ? billAddress.Email : shippingAddress.Email; //TODO: shipping address email might be null if is CVS. Can only send SMS.
            var toName = shippingAddress.LastName + shippingAddress.FirstName;

            return await _workflowMessageService.SendNotificationAsync(messageTemplate, emailAccount, languageId, tokens,
                toEmail, toName, fromEmail: fromEmail, fromName: fromName);
        }

        public async Task SendReturnRequestNumberSmsAsync(bool isCvs, Order order, string returnNumber)
        {
            order.ShippingMethod = isCvs ? order.ShippingMethod.Substring(0, order.ShippingMethod.IndexOf("超商")) : order.ShippingMethod;
            var message = isCvs
                ? string.Format(await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.ReturnRequest.Sms.Cvs"), order.Id, order.ShippingMethod, returnNumber)
                : string.Format(await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.ReturnRequest.Sms.Home"), order.Id, order.ShippingMethod);

            var shippingAddress = await _addressService.GetAddressByIdAsync((int)order.ShippingAddressId);

            await _eventPublisher.PublishAsync(new SendSmsRecordEvent(message, shippingAddress.CellPhone));
        }

        #endregion
    }
}
