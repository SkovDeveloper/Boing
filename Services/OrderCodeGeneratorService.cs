using System.Text.RegularExpressions;

namespace Boing.Services;

public sealed partial class OrderCodeGeneratorService : IOrderCodeGeneratorService
{
    [GeneratedRegex(@"^SkO:(\d+)v$")]
    private static partial Regex CodePattern();

    public string GenerateNextCode(IEnumerable<ProductionOrder> existingOrders)
    {
        var maxNumber = 0;

        foreach (var order in existingOrders)
        {
            var match = CodePattern().Match(order.Code);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > maxNumber)
            {
                maxNumber = parsed;
            }
        }

        return $"SkO:{maxNumber + 1:000}v";
    }
}