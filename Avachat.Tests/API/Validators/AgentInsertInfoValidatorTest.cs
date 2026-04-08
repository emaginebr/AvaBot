using Xunit;
using Avachat.API.Validators;
using Avachat.DTO;

namespace Avachat.Tests.API.Validators;

public class AgentInsertInfoValidatorTest
{
    private readonly AgentInsertInfoValidator _sut;

    public AgentInsertInfoValidatorTest()
    {
        _sut = new AgentInsertInfoValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsValid()
    {
        var info = new AgentInsertInfo
        {
            Name = "Test Agent",
            SystemPrompt = "You are a helpful agent"
        };

        var result = _sut.Validate(info);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenNameIsEmpty(string? name)
    {
        var info = new AgentInsertInfo { Name = name!, SystemPrompt = "prompt" };

        var result = _sut.Validate(info);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaxLength()
    {
        var info = new AgentInsertInfo
        {
            Name = new string('a', 261),
            SystemPrompt = "prompt"
        };

        var result = _sut.Validate(info);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenSystemPromptIsEmpty(string? systemPrompt)
    {
        var info = new AgentInsertInfo { Name = "Agent", SystemPrompt = systemPrompt! };

        var result = _sut.Validate(info);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SystemPrompt");
    }

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenMultipleFieldsInvalid()
    {
        var info = new AgentInsertInfo { Name = "", SystemPrompt = "" };

        var result = _sut.Validate(info);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}
