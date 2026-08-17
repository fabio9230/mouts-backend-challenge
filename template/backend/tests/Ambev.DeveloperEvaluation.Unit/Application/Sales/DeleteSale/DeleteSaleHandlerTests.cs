using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.DeleteSale;

public sealed class DeleteSaleHandlerTests
{
    [Fact(DisplayName = "Should delete a sale")]
    public async Task Handle_Should_Delete_Sale_When_Repository_Returns_True()
    {
        var id = Guid.NewGuid();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .DeleteAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateHandler(repository)
            .Handle(
                new DeleteSaleCommand
                {
                    Id = id
                },
                CancellationToken.None);

        await repository
            .Received(1)
            .DeleteAsync(
                id,
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should throw a exception when sale does not exist")]
    public async Task Handle_Should_Throw_When_Sale_Does_Not_Exist()
    {
        var id = Guid.NewGuid();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .DeleteAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => CreateHandler(repository)
            .Handle(
                new DeleteSaleCommand
                {
                    Id = id
                },
                CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {id} not found.");
    }

    [Fact(DisplayName = "Validator should required id")]
    public async Task Validator_Should_Require_Id()
    {
        var result = await new DeleteSaleValidator()
            .ValidateAsync(new DeleteSaleCommand());

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .ContainSingle(x => x.PropertyName == "Id");
    }

    private static DeleteSaleHandler CreateHandler(
        ISaleRepository repository) =>
        new(
            repository,
            Substitute.For<ILogger<DeleteSaleHandler>>());
}