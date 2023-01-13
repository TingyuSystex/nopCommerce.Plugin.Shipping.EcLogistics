using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public class EcLogisticsReplyModel
    {
        public EcLogisticsReplyModel(IFormCollection form)
        {
            MerchantID          = form["MerchantID"].ToString();
            MerchantTradeNo     = form["MerchantTradeNo"].ToString();
            RtnCode             = form["RtnCode"].ToString();
            RtnMsg              = form["RtnMsg"].ToString();
            AllPayLogisticsID   = form["AllPayLogisticsID"].ToString();
            LogisticsType       = form["LogisticsType"].ToString();
            LogisticsSubType    = form["LogisticsSubType"].ToString();
            GoodsAmount         = form["GoodsAmount"].ToString();
            UpdateStatusDate    = form["UpdateStatusDate"].ToString();
            ReceiverName        = form["ReceiverName"].ToString();
            ReceiverPhone       = form["ReceiverPhone"].ToString();
            ReceiverCellPhone   = form["ReceiverCellPhone"].ToString();
            ReceiverEmail       = form["ReceiverEmail"].ToString();
            ReceiverAddress     = form["ReceiverAddress"].ToString();
            CVSPaymentNo        = form["CVSPaymentNo"].ToString();
            CVSValidationNo     = form["CVSValidationNo"].ToString();
            BookingNote         = form["BookingNote"].ToString();
            CheckMacValue       = form["CheckMacValue"].ToString();
        } 

        public string MerchantID { get; set; }
        
        public string MerchantTradeNo { get; set; }
        
        public string RtnCode { get; set; }
        
        public string RtnMsg { get; set; }
        
        public string AllPayLogisticsID { get; set; }
        
        public string LogisticsType { get; set; }
        
        public string LogisticsSubType { get; set; }
        
        public string GoodsAmount { get; set; }
        
        public string UpdateStatusDate { get; set; }
        
        public string ReceiverName { get; set; }
        
        public string ReceiverPhone { get; set; }
        
        public string ReceiverCellPhone { get; set; }
        
        public string ReceiverEmail { get; set; }
        
        public string ReceiverAddress { get; set; }
        
        public string CVSPaymentNo { get; set; }
        
        public string CVSValidationNo { get; set; }
        
        public string BookingNote { get; set; }
        
        public string CheckMacValue{ get; set; }
    }
}
