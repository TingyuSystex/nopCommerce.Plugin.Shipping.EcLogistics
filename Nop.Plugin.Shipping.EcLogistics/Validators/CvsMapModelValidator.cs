using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Shipping.EcLogistics.Validators
{
    public class CvsMapModelValidator : BaseNopValidator<CvsMapModel>
    {
        public CvsMapModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.ReceiverCellPhone).Must((x, context) =>
            {
                if (string.IsNullOrEmpty(x.ReceiverCellPhone))
                {
                    return false;
                }

                if (Regex.IsMatch(x.ReceiverCellPhone, @"^09[0-9]{8}$"))
                {
                    return true;
                }

                return false;
            }).WithMessageAwait(localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.ReceiverCellPhone.Invalid"));
        }

    }
}
