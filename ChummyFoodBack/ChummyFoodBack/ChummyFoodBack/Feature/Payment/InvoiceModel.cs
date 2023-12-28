using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChummyFoodBack.Feature.Payment;


public enum PricingType
{
    [JsonPropertyName("fixed_price")]
    FixedPrice
}

public class LowerCasePolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
        => name.ToLower();
}
public class LocalPrice
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
}

public class InvoiceModel
{
    [JsonPropertyName("business_name")] 
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; }

    [JsonPropertyName("customer_name")] 
    public string CustomerName { get; set; } = string.Empty;

    public string Memo { get; set; }
    
    [JsonPropertyName("local_price")]
    public LocalPrice LocalPrice { get; set; }
    
}
