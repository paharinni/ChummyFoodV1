using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChummyFoodBack.Factories;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Feature.Payment.Converters;
using ChummyFoodBack.Feature.Payment.Interfaces;
using ChummyFoodBack.Options;
using Microsoft.Extensions.Options;

namespace ChummyFoodBack.Feature.Payment;



public class CommerceResponseData
{
    public string Code { get; set; }

    [JsonPropertyName("hosted_url")]
    public string HostedUrl { get; set; }
}
public class CommerceResponse
{
    public CommerceResponseData Data { get; set; }
}

public class IssueCoinbasePayment: IPaymentAction
{
    private readonly ILogger<IssueCoinbasePayment> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    private readonly string CreateCurrencyPayment 
        = "https://api.commerce.coinbase.com/invoices";
    public IssueCoinbasePayment(
        IOptions<PaymentOptions> paymentOptions,
        ILogger<IssueCoinbasePayment> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = paymentOptions.Value.ApiKey;
    }

    public async Task<PaymentResponse> CreateCharge(PaymentRequestModel payment, string checkoutName)
    {
        CoinbaseHttpClientFactory.AddCoinbaseSecurity(_apiKey, _httpClient);
        
        using var chargeRequest = CreateChargeRequest();
        //Message handler will be returned to pool

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(),},
            PropertyNamingPolicy = new LowerCasePolicy()
        };

        var chargeModel = new InvoiceModel
        {
            LocalPrice = new LocalPrice
            {
                Amount = payment.Amount.TotalAmount,
                Currency = Currency.USD
            },
            CustomerEmail = payment.Email,
            BusinessName = checkoutName,
            CustomerName = "Arbitrary customer"
        };
        
        chargeRequest.Content = JsonContent.Create(chargeModel, 
            new MediaTypeHeaderValue("application/json"), serializerOptions);
        var result = await _httpClient.SendAsync(chargeRequest);
        if (result.StatusCode is HttpStatusCode.Created)
        {
            var response = await result.Content.ReadAsStringAsync();
            
            CommerceResponse responseMessage = 
                (await result.Content.ReadFromJsonAsync<CommerceResponse>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }))!;
            return new PaymentResponse
            {
                ChargeCode = responseMessage.Data.Code,
                PaymentURL = responseMessage.Data.HostedUrl
            };
        }

        var errorResponse = await result.Content.ReadAsStringAsync();
        _logger.LogError("Failed response payload" + Environment.NewLine + errorResponse);
        
        return new PaymentResponse
        {
            ChargeCode = "ERROR",
            PaymentURL = "Mock payment url"
        };
        // throw new PaymentException("Failed to create invoice");
    }

    HttpRequestMessage CreateChargeRequest()
    {
        var chargeRequest = new HttpRequestMessage(HttpMethod.Post, CreateCurrencyPayment);
        chargeRequest.Headers.Add("Accept", "application/json");
        return chargeRequest;
    }
}