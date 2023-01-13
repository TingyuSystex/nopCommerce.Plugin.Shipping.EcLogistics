using Nop.Core;

namespace Nop.Plugin.Shipping.EcLogistics.Domain
{
    public class EcPayReturnNumberMapping : BaseEntity
    {
        /// <summary>
        /// Gets or sets the return request identifier
        /// </summary>
        public int ReturnRequestId { get; set; }

        /// <summary>
        /// Gets or sets the address identifier
        /// </summary>
        public string ReturnNumber { get; set; }
        
    }
}
