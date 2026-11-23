using System.Text.Json;
using Sarsoo.Agent.Context.Hindsight.Model;

namespace Sarsoo.Agent.Context.Hindsight;

public class HindsightClient(IHttpClientFactory factory)
{
    public const string HindsightHTTPClientName = "hindsight";

    public async Task<RetainResponse> Retain(string bankId, RetainRequest request, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateClient(HindsightHTTPClientName);

        var serial = JsonSerializer.Serialize<RetainRequest>(request, HindsightJsonContext.Default.RetainRequest);
        var req = new StringContent(serial);

        var response = await client.PostAsync($"/v1/default/banks/{bankId}/memories",  req, cancellationToken: cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var responseObject = await response.Content.ReadFromJsonAsync<RetainResponse>(HindsightJsonContext.Default.RetainResponse, cancellationToken).ConfigureAwait(false);

        return responseObject ?? throw new InvalidCastException();
    }

    public async Task<RecallResponse> Recall(string bankId, RecallRequest request, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateClient(HindsightHTTPClientName);

        var serial = JsonSerializer.Serialize<RecallRequest>(request, HindsightJsonContext.Default.RecallRequest);
        var req = new StringContent(serial);

        var response = await client.PostAsync($"/v1/default/banks/{bankId}/memories/recall",  req, cancellationToken: cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var responseObject = await response.Content.ReadFromJsonAsync<RecallResponse>(HindsightJsonContext.Default.RecallResponse, cancellationToken).ConfigureAwait(false);

        return responseObject ?? throw new InvalidCastException();
    }

    public async Task<ReflectResponse> Reflect(string bankId, ReflectRequest request, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateClient(HindsightHTTPClientName);

        var serial = JsonSerializer.Serialize<ReflectRequest>(request, HindsightJsonContext.Default.ReflectRequest);
        var req = new StringContent(serial);

        var response = await client.PostAsync($"/v1/default/banks/{bankId}/reflect",  req, cancellationToken: cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var responseObject = await response.Content.ReadFromJsonAsync<ReflectResponse>(HindsightJsonContext.Default.ReflectResponse, cancellationToken).ConfigureAwait(false);

        return responseObject ?? throw new InvalidCastException();
    }
}