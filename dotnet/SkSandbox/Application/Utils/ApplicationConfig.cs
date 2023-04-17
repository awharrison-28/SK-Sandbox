// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Utils;

[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
    Justification = "Configuration classes are instantiated through IConfiguration.")]
internal sealed class ApplicationConfig
{
    [JsonPropertyName("embedding_endpoint")]
    public string EmbeddingEndpoint { get; set; }

    [JsonPropertyName("embedding_api_key")]
    public string EmbeddingApiKey { get; set; }

    [JsonPropertyName("completion_endpoint")]
    public string CompletionEndpoint { get; set; }

    [JsonPropertyName("completion_api_key")]
    public string CompletionApiKey { get; set; }

    [JsonConstructor]
    public ApplicationConfig(string embeddingEndpoint, string embeddingApiKey, string completionEndpoint, string completionApiKey)
    {
        this.EmbeddingEndpoint = embeddingEndpoint;
        this.EmbeddingApiKey = embeddingApiKey;
        this.CompletionEndpoint = completionEndpoint;
        this.CompletionApiKey = completionApiKey;
    }
}