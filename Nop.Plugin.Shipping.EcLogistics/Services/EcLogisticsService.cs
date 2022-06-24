using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Factories;

namespace Nop.Plugin.Shipping.EcLogistics.Services
{
    public class EcLogisticsService
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly EcLogisticsSettings _ecLogisticsSettings;
        private readonly ICommonModelFactory _commonModelFactory;
        private readonly IStoreService _storeService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IWebHelper _webHelper;
        private readonly IPictureService _pictureService;

        #endregion

        #region Ctor

        public EcLogisticsService(HttpClient client,
            EcLogisticsSettings ecLogisticsSettings,
            ICommonModelFactory commonModelFactory,
            IStoreService storeService,
            IStaticCacheManager staticCacheManager,
            IWebHelper webHelper,
            IPictureService pictureService)
        {
            //configure client
            client.Timeout = TimeSpan.FromSeconds(25);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"nopCommerce-{NopVersion.CURRENT_VERSION}");

            _httpClient = client;
            _ecLogisticsSettings = ecLogisticsSettings;
            _commonModelFactory = commonModelFactory;
            _storeService = storeService;
            _staticCacheManager = staticCacheManager;
            _webHelper = webHelper;
            _pictureService = pictureService;
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

        #endregion

        public async Task<bool> SendTestRequestAsync(string subType)
        {
            using (var httpClient = new HttpClient())
            {
                var baseUri = "https://logistics-stage.ecpay.com.tw/Express/CreateTestData";

                var merchantId = _ecLogisticsSettings.MerchantId;
                var hashKey = _ecLogisticsSettings.HashKey;
                var hashIV = _ecLogisticsSettings.HashIV;
                subType = subType.ToUpper();

                var queryStr = "LogisticsSubType=" + subType + "&MerchantID=" + merchantId;
                var cmv = generateCheckMacValue(queryStr, hashKey, hashIV);

                var parameters = new Dictionary<string, string>();
                parameters.Add("MerchantID",  merchantId);
                parameters.Add("LogisticsSubType", subType);
                parameters.Add("CheckMacValue", cmv);

                var httpContent = new FormUrlEncodedContent(parameters);

                httpClient.BaseAddress = new Uri(baseUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                HttpResponseMessage httpResponse = httpClient.PostAsync("", httpContent).Result;
                var response = httpResponse.Content.ReadAsStringAsync().Result;
                var result = response.Substring(0, 1);

                return httpResponse.IsSuccessStatusCode && int.Parse(result) == 1;
            }
        }
        
    }
}
