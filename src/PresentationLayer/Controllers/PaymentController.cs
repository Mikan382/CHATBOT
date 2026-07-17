using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;

namespace PresentationLayer.Controllers;

[Authorize]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(Guid planId, CancellationToken cancellationToken)
    {
        try
        {
            var checkoutUrl = await _paymentService.CreateCheckoutAsync(
                CurrentUserId(),
                planId,
                ResolveClientIp(),
                BuildReturnUrl(),
                cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
            return RedirectToAction("Index", "Subscriptions");
        }
    }

    // VNPay redirects the browser here after payment. Confirmation relies on the signed callback,
    // not on an authentication cookie, because the browser session may have expired meanwhile.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VnpayReturn(CancellationToken cancellationToken)
    {
        var result = await _paymentService.ConfirmAsync(ReadCallback(), cancellationToken);
        if (result.Success)
        {
            SetFlashSuccess(result.Message);
        }
        else
        {
            SetFlashError(result.Message);
        }

        return RedirectToAction("Index", "Subscriptions");
    }

    // Server-to-server IPN from VNPay (no auth cookie). Authoritative confirmation; must answer with
    // VNPay's expected JSON so the gateway stops retrying.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VnpayIpn(CancellationToken cancellationToken)
    {
        var result = await _paymentService.ConfirmAsync(ReadCallback(), cancellationToken);
        return Json(new { RspCode = result.IpnResponseCode, Message = result.IpnMessage });
    }

    private IReadOnlyDictionary<string, string> ReadCallback()
    {
        return Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
    }

    private string ResolveClientIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress
            ?? throw new InvalidOperationException("The client IP address is unavailable.");
        if (IPAddress.IsLoopback(ip))
        {
            return IPAddress.Loopback.ToString();
        }

        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString();
    }

    private string BuildReturnUrl()
    {
        return Url.Action(
                nameof(VnpayReturn),
                "Payment",
                values: null,
                protocol: Request.Scheme,
                host: Request.Host.Value)
            ?? throw new InvalidOperationException("Could not create the VNPay return URL.");
    }
}
