using Nop.Core;

namespace Nop.Plugin.Shipping.EcLogistics.Domain
{
    public class EcPayCvsAddressMapping : BaseEntity
    {
        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the address identifier
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// Gets or sets the LogisticsSubType
        /// </summary>
        public string CvsType { get; set; }
    }
}
