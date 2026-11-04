using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Manutenzione.Api.Contracts;
using Manutenzione.Api.Domain;

namespace Manutenzione.Api.Tests;

public class WorkOrderTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client;

    public WorkOrderTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Creating_an_asset_with_a_duplicate_code_returns_conflict()
    {
        await CreateAsset("PRS-01");

        var response = await _client.PostAsJsonAsync("/api/assets", new CreateAssetRequest("prs-01", "Pressa", null), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_request_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/assets", new CreateAssetRequest("", "", null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Work_order_cannot_start_without_a_technician()
    {
        var asset = await CreateAsset("CNC-02");
        var order = await CreateOrder(asset.Id, new DateOnly(2026, 3, 20));

        var response = await _client.PutAsJsonAsync($"/api/work-orders/{order.Id}/status", new ChangeStatusRequest(WorkOrderStatus.InProgress, null), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Work_order_follows_its_lifecycle_until_completion()
    {
        var asset = await CreateAsset("CMP-03");
        var order = await CreateOrder(asset.Id, new DateOnly(2026, 3, 20));
        var technician = await Post<TechnicianResponse>("/api/technicians", new CreateTechnicianRequest("Mario Rossi", "Meccanica"));

        await Put($"/api/work-orders/{order.Id}/technician", new AssignTechnicianRequest(technician.Id));
        await Put($"/api/work-orders/{order.Id}/status", new ChangeStatusRequest(WorkOrderStatus.InProgress, null));
        var completed = await Put($"/api/work-orders/{order.Id}/status", new ChangeStatusRequest(WorkOrderStatus.Completed, "Sostituito filtro"));

        Assert.Equal(WorkOrderStatus.Completed, completed.Status);
        Assert.Equal(ApiFactory.Now.UtcDateTime, completed.CompletedAt);

        var reopen = await _client.PutAsJsonAsync($"/api/work-orders/{order.Id}/status", new ChangeStatusRequest(WorkOrderStatus.InProgress, null), Json);
        Assert.Equal(HttpStatusCode.Conflict, reopen.StatusCode);
    }

    [Fact]
    public async Task Overdue_filter_returns_only_open_orders_past_their_due_date()
    {
        var asset = await CreateAsset("FRN-04");
        var late = await CreateOrder(asset.Id, new DateOnly(2026, 3, 1));
        await CreateOrder(asset.Id, new DateOnly(2026, 4, 1));

        var overdue = await _client.GetFromJsonAsync<List<WorkOrderResponse>>($"/api/work-orders?overdue=true&assetId={asset.Id}", Json);

        var only = Assert.Single(overdue!);
        Assert.Equal(late.Id, only.Id);
        Assert.True(only.Overdue);
    }

    [Fact]
    public async Task Generating_plans_creates_one_order_per_due_date_and_is_idempotent()
    {
        var asset = await CreateAsset("CMP-05");
        await Post<PlanResponse>("/api/plans", new CreatePlanRequest(asset.Id, "Controllo lubrificazione", 7, WorkOrderPriority.High, new DateOnly(2026, 3, 10)));

        var first = await _client.PostAsync("/api/plans/generate?daysAhead=14", null);
        var result = await first.Content.ReadFromJsonAsync<GenerateResult>(Json);
        var second = await (await _client.PostAsync("/api/plans/generate?daysAhead=14", null)).Content.ReadFromJsonAsync<GenerateResult>(Json);

        Assert.Equal(3, result!.Created);
        Assert.Equal(0, second!.Created);

        var orders = await _client.GetFromJsonAsync<List<WorkOrderResponse>>($"/api/work-orders?assetId={asset.Id}", Json);
        Assert.Equal([new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 17), new DateOnly(2026, 3, 24)], orders!.Select(o => o.DueDate));
    }

    private Task<AssetResponse> CreateAsset(string code) =>
        Post<AssetResponse>("/api/assets", new CreateAssetRequest(code, $"Macchina {code}", "Reparto A"));

    private Task<WorkOrderResponse> CreateOrder(int assetId, DateOnly due) =>
        Post<WorkOrderResponse>("/api/work-orders", new CreateWorkOrderRequest(assetId, "Ispezione", null, WorkOrderPriority.Medium, due));

    private async Task<T> Post<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<WorkOrderResponse> Put(string url, object body)
    {
        var response = await _client.PutAsJsonAsync(url, body, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkOrderResponse>(Json))!;
    }
}
