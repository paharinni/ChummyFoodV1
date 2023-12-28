using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChummyFoodBack.Feature.Payment.Converters;

public class PricingTypeConverter: JsonConverter<PricingType>
{
    public override PricingType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? jsonChunk = reader.GetString();
        if (jsonChunk is null)
        {
            return PricingType.FixedPrice;
        }

        return jsonChunk switch
        {
            "fixed_price" => PricingType.FixedPrice,
            _ => throw new JsonException("Unable to deserialize fixed price chunk with value: " + jsonChunk)
        };
    }

    public override void Write(Utf8JsonWriter writer, PricingType value, JsonSerializerOptions options)
    {
        var resultString = value switch
        {
            PricingType.FixedPrice => "fixed_price"
        };
        
        writer.WriteStringValue(resultString);
    }
}