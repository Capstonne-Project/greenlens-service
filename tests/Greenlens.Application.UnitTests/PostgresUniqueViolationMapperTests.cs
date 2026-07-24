using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class PostgresUniqueViolationMapperTests
{
    [Fact]
    public void TryMap_EmailIndex_ReturnsEmailTaken()
    {
        var ex = new Exception("duplicate key value violates unique constraint \"ix_users_email\"");

        var error = PostgresUniqueViolationMapper.TryMap(ex);

        Assert.NotNull(error);
        Assert.Equal("EMAIL_TAKEN", error!.Code);
    }

    [Fact]
    public void TryMap_ContractNumberIndex_ReturnsCompanyContractExists()
    {
        var ex = new Exception("duplicate key value violates unique constraint \"ix_environmental_service_companies_contract_number\"");

        var error = PostgresUniqueViolationMapper.TryMap(ex);

        Assert.NotNull(error);
        Assert.Equal("COMPANY_CONTRACT_NUMBER_EXISTS", error!.Code);
    }

    [Fact]
    public void TryMap_NonUniqueError_ReturnsNull()
    {
        var ex = new Exception("connection timeout");

        var error = PostgresUniqueViolationMapper.TryMap(ex);

        Assert.Null(error);
    }
}
