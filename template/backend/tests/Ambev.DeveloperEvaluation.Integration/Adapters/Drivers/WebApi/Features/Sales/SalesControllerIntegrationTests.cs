using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Adapters.Drivers.WebApi.Features.Sales;

[Collection(PostgreSqlCollection.Name)]
public sealed class SalesControllerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private SalesApiFactory _factory = null!;
    private HttpClient _client = null!;

    public SalesControllerIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new SalesApiFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "Should create a sale and return the traceId")]
    public async Task PostSale_Should_Create_Sale_And_Return_TraceId()
    {
        var request = CreateRequest();
        using var httpRequest = CreatePostRequest(request, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.TryGetValues("X-Trace-Id", out var traceIds).Should().BeTrue();
        traceIds!.Single().Should().NotBeNullOrWhiteSpace();

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.SaleNumber.Should().Be(request.SaleNumber);
        body.Data.Items.Should().ContainSingle();
        body.Data.Items.Single().DiscountPercentage.Should().Be(10m);
        body.Data.Items.Single().TotalAmount.Should().Be(360m);
    }

    [Fact(DisplayName = "Should return the same sale when create a sale with same idempotency key")]
    public async Task PostSale_With_Same_IdempotencyKey_And_Same_Request_Should_Return_Same_Sale()
    {
        var request = CreateRequest();
        var key = Guid.NewGuid().ToString();

        using var firstRequest = CreatePostRequest(request, key);
        var firstResponse = await _client.SendAsync(firstRequest);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();

        using var secondRequest = CreatePostRequest(request, key);
        var secondResponse = await _client.SendAsync(secondRequest);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.Headers.TryGetValues("X-Idempotent-Replay", out var replayHeaders).Should().BeTrue();
        replayHeaders!.Single().Should().Be("true");

        firstBody!.Data!.Id.Should().Be(secondBody!.Data!.Id);
        firstBody.Data.SaleNumber.Should().Be(secondBody.Data.SaleNumber);
    }

    [Fact(DisplayName = "Should return conflict when request is diferent and same idempotency key")]
    public async Task PostSale_With_Same_IdempotencyKey_And_Different_Request_Should_Return_Conflict()
    {
        var key = Guid.NewGuid().ToString();
        var first = CreateRequest();
        var second = CreateRequest();

        using var firstRequest = CreatePostRequest(first, key);
        var firstResponse = await _client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var secondRequest = CreatePostRequest(second, key);
        var secondResponse = await _client.SendAsync(secondRequest);
        var body = await secondResponse.Content.ReadFromJsonAsync<ApiResponse>();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body!.Message.Should().Contain("Idempotency-Key");
    }

    [Fact(DisplayName = "Should return conflict when sale number is duplicate and diferent idempotency key")]
    public async Task PostSale_With_Duplicate_SaleNumber_And_Different_IdempotencyKey_Should_Return_Conflict()
    {
        var first = CreateRequest();
        var second = CreateRequest();
        second.SaleNumber = first.SaleNumber;

        using var firstRequest = CreatePostRequest(first, Guid.NewGuid().ToString());
        var firstResponse = await _client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var secondRequest = CreatePostRequest(second, Guid.NewGuid().ToString());
        var secondResponse = await _client.SendAsync(secondRequest);
        var body = await secondResponse.Content.ReadFromJsonAsync<ApiResponse>();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body!.Message.Should().Contain(first.SaleNumber);
    }

    [Fact(DisplayName = "Should update existing item")]
    public async Task UpdateSale_Should_Update_Existing_Item()
    {
        var requestCreate = CreateRequest(1);
        using var httpRequest = CreatePostRequest(requestCreate, Guid.NewGuid().ToString());
        var responseCreate = await _client.SendAsync(httpRequest);
        var body = await responseCreate.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();
        var sale = body!.Data!;
        var existingItem = sale.Items.Single();

        var request = new UpdateSaleRequest
        {
            SaleNumber = sale.SaleNumber,
            Date = sale.Date,
            CustomerId = sale.CustomerId,
            BranchId = sale.BranchId,
            Items =
            [
                new UpdateSaleItemRequest
                {
                    Id = existingItem.Id,
                    ProductId = existingItem.ProductId,
                    ProductName = "Mouse Updated",
                    Quantity = 5,
                    UnitPrice = 100m
                }
            ]
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/Sales/{sale.Id}",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponseWithData<SaleResponse>>();

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        result.Data!.Items.Should().ContainSingle();

        var item = result.Data.Items.Single();

        item.Id.Should().Be(existingItem.Id);
        item.Quantity.Should().Be(5);
        item.DiscountPercentage.Should().Be(10m);
        item.TotalAmount.Should().Be(450m);
    }

    [Fact(DisplayName = "Should add new item when update request has new item")]
    public async Task UpdateSale_Should_Add_New_Item()
    {
        var requestCreate = CreateRequest(1);
        using var httpRequest = CreatePostRequest(requestCreate, Guid.NewGuid().ToString());
        var responseCreate = await _client.SendAsync(httpRequest);
        var body = await responseCreate.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();
        var sale = body!.Data!;
        var existingItem = sale.Items.Single();

        var newProductId = Guid.NewGuid();

        var request = new UpdateSaleRequest
        {
            SaleNumber = sale.SaleNumber,
            Date = sale.Date,
            CustomerId = sale.CustomerId,
            BranchId = sale.BranchId,
            Items =
            [
                new UpdateSaleItemRequest
                {
                    Id = null,
                    ProductId = newProductId,
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 200m
                }
            ]
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/Sales/{sale.Id}",
            request);

        var bodyResult = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, bodyResult);

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponseWithData<SaleResponse>>();

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        result.Data!.Items.Should().HaveCount(2);

        var item = result.Data.Items[1];

        item.ProductId.Should().Be(newProductId);
        item.ProductName.Should().Be("Keyboard");
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(200m);
        item.DiscountPercentage.Should().Be(0m);
        item.TotalAmount.Should().Be(400m);
    }

    [Fact(DisplayName = "Should update existing item and add new item")]
    public async Task UpdateSale_Should_Update_Existing_And_Add_New_Item()
    {

        var requestCreate = CreateRequest(2);
        using var httpRequest = CreatePostRequest(requestCreate, Guid.NewGuid().ToString());
        var responseCreate = await _client.SendAsync(httpRequest);
        var body = await responseCreate.Content.ReadFromJsonAsync<ApiResponseWithData<SaleResponse>>();
        var sale = body!.Data!;
        var existingItem = sale.Items.Single();

        var newProductId = Guid.NewGuid();

        var request = new UpdateSaleRequest
        {
            SaleNumber = sale.SaleNumber,
            Date = sale.Date,
            CustomerId = sale.CustomerId,
            BranchId = sale.BranchId,
            Items =
            [
                // EXISTENTE → UPDATE
                new UpdateSaleItemRequest
                {
                    Id = existingItem.Id,
                    ProductId = existingItem.ProductId,
                    ProductName = "Mouse Updated",
                    Quantity = 5,
                    UnitPrice = 100m
                },

                // NOVO → INSERT
                new UpdateSaleItemRequest
                {
                    Id = null,
                    ProductId = newProductId,
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 200m
                }
            ]
        };


        var response = await _client.PutAsJsonAsync(
            $"/api/Sales/{sale.Id}",
            request);

        var bodyResult = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, bodyResult);

        var result =
            await response.Content.ReadFromJsonAsync<
                ApiResponseWithData<SaleResponse>>();

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        var updatedSale = result.Data!;

        updatedSale.Items.Should().HaveCount(2);

        // Existing → UPDATE
        var updatedItem = updatedSale.Items.Single(x =>
            x.Id == existingItem.Id);

        updatedItem.ProductName.Should().Be("Mouse Updated");
        updatedItem.Quantity.Should().Be(5);
        updatedItem.UnitPrice.Should().Be(100m);
        updatedItem.DiscountPercentage.Should().Be(10m);
        updatedItem.TotalAmount.Should().Be(450m);

        // New → INSERT
        var newItem = updatedSale.Items.Single(x =>
            x.ProductId == newProductId);

        newItem.Id.Should().NotBeEmpty();
        newItem.ProductName.Should().Be("Keyboard");
        newItem.Quantity.Should().Be(2);
        newItem.UnitPrice.Should().Be(200m);
        newItem.DiscountPercentage.Should().Be(0m);
        newItem.TotalAmount.Should().Be(400m);

        // Sale total
        updatedSale.TotalAmount.Should().Be(850m);
    }

    private static HttpRequestMessage CreatePostRequest(CreateSaleRequest request, string idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/Sales")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        return message;
    }

    private static CreateSaleRequest CreateRequest(int quantity = 4)
    {
        return new CreateSaleRequest
        {
            SaleNumber = $"INT-API-{Guid.NewGuid():N}",
            Date = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            Items =
            [
                new CreateSaleItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = quantity,
                    UnitPrice = 100m
                }
            ]
        };
    }
}
