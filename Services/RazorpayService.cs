using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace AcademyNet.Services;

public class RazorpayOptions
{
    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(KeySecret);
}

/// <summary>Razorpay Orders API + signature checks. Without keys the site runs in demo-payment mode.</summary>
public class RazorpayService(HttpClient http, Microsoft.Extensions.Options.IOptions<RazorpayOptions> options)
{
    private readonly RazorpayOptions _o = options.Value;
    public RazorpayOptions Options => _o;

    /// <summary>Creates a Razorpay order for the amount (in rupees) and returns its id.</summary>
    public async Task<string> CreateOrderAsync(int amountInr, string receipt, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_o.KeyId}:{_o.KeySecret}")));
        var body = new JsonObject { ["amount"] = amountInr * 100, ["currency"] = "INR", ["receipt"] = receipt };
        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var res = await http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Razorpay error {(int)res.StatusCode}: {text}");
        return JsonNode.Parse(text)!["id"]!.GetValue<string>();
    }

    /// <summary>Checkout signature = HMAC_SHA256(order_id + "|" + payment_id, key_secret).</summary>
    public bool VerifyPayment(string orderId, string paymentId, string signature)
        => Matches($"{orderId}|{paymentId}", _o.KeySecret, signature);

    /// <summary>Webhook signature = HMAC_SHA256(raw body, webhook secret).</summary>
    public bool VerifyWebhook(string body, string signature)
        => !string.IsNullOrEmpty(_o.WebhookSecret) && Matches(body, _o.WebhookSecret, signature);

    private static bool Matches(string payload, string secret, string signature)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature)) return false;
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.ToLowerInvariant()));
    }
}
