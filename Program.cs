public enum SubscriptionStatus
{
    Trial,
    Basic,
    Pro,
    Student
}

public class Subscriber
{
    public string Id { get; private set; }
    public string Region { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public int TenureMonths { get; private set; }
    public int Devices { get; private set; }
    public double BasePrice { get; private set; }

    public Subscriber(
        string id,
        string region,
        SubscriptionStatus status,
        int tenureMonths,
        int devices,
        double basePrice)
    {
        
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException("Id required", nameof(id));

        if (region is null)
            throw new ArgumentNullException("Region required", nameof(region));

        if (basePrice < 0)
            throw new ArgumentOutOfRangeException(
                "Base price cannot be negative", 
                nameof(basePrice)
            );

        Id = id;
        Region = region;
        Status = status;
        TenureMonths = tenureMonths;
        Devices = devices;
        BasePrice = basePrice;
    }
}

public class BillingService
{
    private static double ApplyStatusDiscount(
        SubscriptionStatus status,
        int tenure,
        double basePrice) => status switch
    {
        SubscriptionStatus.Trial => 0,
        SubscriptionStatus.Student => basePrice * 0.5,
        SubscriptionStatus.Pro when tenure >= 24 => basePrice * 0.85,
        SubscriptionStatus.Pro when tenure >= 12 => basePrice * 0.9,
        SubscriptionStatus.Pro => basePrice,
        _ => basePrice
    };

    private static double AddDeviceSurcharge(double price, int devices) =>
        devices > 3 ? price + 4.99 : price;

    private static double ApplyTax(double price, string region) => region switch
    {
        "EU" => price * 1.21,
        "US" => price * 1.07,
        _ => price
    };

    public (bool Ok, string Error) Validate(Subscriber s)
    {
        if (s is null) return (false, "No subscriber");
        if (string.IsNullOrWhiteSpace(s.Id)) return (false, "Id missing");
        if (s.BasePrice < 0) return (false, "Price < 0");

        return (true, "");
    }

    public double CalcTotal(Subscriber s)
    {
        if (s is null)  
            throw new ArgumentNullException(nameof(s));

        double PriceAfterStatus() => ApplyStatusDiscount(s.Status, s.TenureMonths, s.BasePrice);
        double WithDevices(double x) => AddDeviceSurcharge(x, s.Devices);
        double WithTax(double x) => ApplyTax(x, s.Region);

        return WithTax(WithDevices(PriceAfterStatus()));
    }
}


class Program
{
    static void Main()
    {
        var billing = new BillingService();

        var subscribers = new List<Subscriber>
        {
            new Subscriber("A-1", "EU", SubscriptionStatus.Trial, 0, 1, 9.99),
            new Subscriber("B-2", "US", SubscriptionStatus.Pro, 18, 4, 14.99),
            new Subscriber("C-3", "EU", SubscriptionStatus.Student, 6, 2, 12.99)
        };

        foreach (var sub in subscribers)
        {
            var (ok, error) = billing.Validate(sub);
            if (ok)
            {
                double total = billing.CalcTotal(sub);
                Console.WriteLine($"Subscriber {sub.Id}: ${total:0.00}");
            }
            else
            {
                Console.WriteLine($"Validation failed for {sub.Id}: {error}");
            }
        }
    }
}
