using System.Net.Mime;
// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Utils;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Diagnostics;

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.TextEmbedding;

/// <summary>
/// HuggingFace embedding generation service.
/// </summary>
public sealed class HFEmbeddingInferenceEndpoint : IEmbeddingGeneration<string, float>, IDisposable
{
    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler? _httpClientHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="HFEmbeddingInferenceEndpoint"/> class.
    /// Using default <see cref="HttpClientHandler"/> implementation.
    /// </summary>
    /// <param name="endpoint">Endpoint for service API call.</param>
    /// <param name="model">Model to use for service API call.</param>
    public HFEmbeddingInferenceEndpoint(Uri endpoint, string apiKey)
    {
        this._endpoint = endpoint;

        this._httpClientHandler = new() { CheckCertificateRevocationList = true };
        this._httpClient = new(this._httpClientHandler);
        this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc/>
    public async Task<IList<Embedding<float>>> GenerateEmbeddingsAsync(IList<string> data)
    {
        return await this.ExecuteEmbeddingRequestAsync(data);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._httpClient.Dispose();
        this._httpClientHandler?.Dispose();
    }

    #region private ================================================================================

    /// <summary>
    /// Performs HTTP request to given endpoint for embedding generation.
    /// </summary>
    /// <param name="data">Data to embed.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated embeddings.</returns>
    /// <exception cref="AIException">Exception when backend didn't respond with generated embeddings.</exception>
    private async Task<IList<Embedding<float>>> ExecuteEmbeddingRequestAsync(IList<string> data, CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddingRequest = new HFEmbeddingRequest
            {
                Inputs = data
            };

            using var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = this._endpoint,
                Content = new StringContent(JsonSerializer.Serialize(embeddingRequest), new MediaTypeWithQualityHeaderValue("application/json"))
            };

            var response = await this._httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var embeddingResponse = JsonSerializer.Deserialize<HFEmbeddingResponse>(body);

            return embeddingResponse?.Embeddings?.Select(l => new Embedding<float>(l.ToArray())).ToList()!;
        }
        catch (Exception e)
        {
            throw new AIException(
                AIException.ErrorCodes.UnknownError,
                $"Something went wrong: {e.Message}", e);
        }
    }

    #endregion
}