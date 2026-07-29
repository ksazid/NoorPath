using NoorPath.Catalogue;

namespace NoorPath.Catalogue.Infrastructure;

public sealed class TestOperatorApproval(IEnumerable<string> operatorIds) : IOperatorApproval
{
    private readonly HashSet<string> approved = operatorIds.ToHashSet(StringComparer.Ordinal);
    public bool IsApprovedTestOperator(string operatorId) => approved.Contains(operatorId);
}
