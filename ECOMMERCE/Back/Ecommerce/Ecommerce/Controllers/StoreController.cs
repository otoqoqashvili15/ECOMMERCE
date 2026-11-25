using Ecommerce.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/store")]
public sealed class StoreController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("checkout-settings")]
    public ActionResult<CheckoutSettingsDto> GetCheckoutSettings() => Ok(new CheckoutSettingsDto(
        configuration.GetValue<decimal>("Store:TaxRate", 0.08m),
        configuration.GetValue<decimal>("Store:StandardShipping", 5m),
        configuration.GetValue<decimal>("Store:ExpressShipping", 15m),
        configuration.GetValue<decimal>("Store:FreeShippingThreshold", 100m)));
}
