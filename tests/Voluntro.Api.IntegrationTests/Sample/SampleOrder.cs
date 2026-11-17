using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// The sample module's persisted aggregate. It exists to exercise every persistence convention at least once:
/// a strongly-typed key and foreign key, a multi-word name (<c>customer_name</c>), an enum stored as text,
/// a nullable column, <see cref="AggregateRoot{TId}.Version"/> on <c>xmin</c>, and a child entity with its own ID.
/// </summary>
public sealed class SampleOrder : AggregateRoot<SampleOrderId>
{
    private readonly List<SampleOrderLine> _lines = [];

    // Also the constructor EF materializes through: the parameter names match the mapped properties.
    private SampleOrder(SampleOrderId id, string customerName, string? note)
        : base(id)
    {
        CustomerName = customerName;
        Note = note;
    }

    public string CustomerName { get; private set; }

    public SampleOrderStatus Status { get; private set; }

    public string? Note { get; private set; }

    public IReadOnlyList<SampleOrderLine> Lines => _lines.AsReadOnly();

    public static SampleOrder Create(string customerName, string? note) => new(SampleOrderId.New(), customerName, note);

    public void Update(string customerName, string? note)
    {
        CustomerName = customerName;
        Note = note;
    }

    public SampleOrderLine AddLine(string productName, int quantity)
    {
        var line = new SampleOrderLine(SampleOrderLineId.New(), Id, productName, quantity);
        _lines.Add(line);
        return line;
    }

    public void Place() => Status = SampleOrderStatus.Placed;
}

public enum SampleOrderStatus
{
    Draft,
    Placed,
}
