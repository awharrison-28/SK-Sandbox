#region Assembly Microsoft.SemanticKernel, Version=0.11.146.1, Culture=neutral, PublicKeyToken=null
// Microsoft.SemanticKernel.dll
#endregion

#nullable enable

using System.Text.Json.Serialization;

namespace Application.Utils;

public sealed class HFEmbeddingResponse
{
    [JsonPropertyName("embeddings")]
    public IEnumerable<float[]>? Embeddings { get; set; }
}
