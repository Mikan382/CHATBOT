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
                cancellationToken);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
            return RedirectToAction("Index", "Subscriptions");
        }
    }

    // VNPay redirects the student's browser back here after payment (GET). The user is still
    // authenticated, so this both confirms the payment and shows the result.
    [Authorize(Roles = "Student")]
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
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) || ip == "::1" ? "127.0.0.1" : ip;
    }
}
