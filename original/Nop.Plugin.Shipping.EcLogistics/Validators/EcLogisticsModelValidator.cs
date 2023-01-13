using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Shipping.EcLogistics.Validators
{
    public class EcLogisticsModelValidator : BaseNopValidator<EcLogisticsModel>
    {
        public EcLogisticsModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.MerchantId)
                .NotEmpty()
                .NotNull()
                .WithMessageAwait(
                    localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Fields.MerchantId.Required"));

            RuleFor(model => model.HashKey)
                .NotEmpty()
                .NotNull()
                .WithMessageAwait(
                    localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Fields.HashKey.Required"));
            
            RuleFor(model => model.HashIV)
                .NotEmpty()
                .NotNull()
                .WithMessageAwait(
                    localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Fields.HashIV.Required"));
        }

    }
}
