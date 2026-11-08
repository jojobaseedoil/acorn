namespace Acorn.Dataverse.Core.Tests;

public class DomainExceptionTests
{
    [Fact]
    public void BusinessRuleException_KeepsMessageAndCode()
    {
        var ex = new BusinessRuleException("Credit limit too high.", "CREDIT_LIMIT");

        Assert.Equal("Credit limit too high.", ex.Message);
        Assert.Equal("CREDIT_LIMIT", ex.Code);
    }

    [Fact]
    public void RecordNotFoundException_DescribesRecord()
    {
        var id = Guid.NewGuid();

        var ex = new RecordNotFoundException("account", id);

        Assert.Equal("account", ex.EntityLogicalName);
        Assert.Equal(id, ex.Id);
        Assert.Contains(id.ToString(), ex.Message);
    }
}