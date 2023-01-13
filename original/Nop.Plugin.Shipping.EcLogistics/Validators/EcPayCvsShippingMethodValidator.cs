using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Shipping.EcLogistics.Validators
{
    public class EcPayCvsShippingMethodValidator : BaseNopValidator<EcPayCvsShippingMethod>
    {
        public EcPayCvsShippingMethodValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.Description)
                .NotEmpty()
                .NotNull()
                .WithMessageAwait(
                    localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Fields.Description.Required"));
            RuleFor(model => model.Description)
                .Length(1, 100)
                .WithMessageAwait(
                    localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Fields.Description.LengthError"));
        }

    }
}
