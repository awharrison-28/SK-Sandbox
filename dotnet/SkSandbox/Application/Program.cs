using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.HuggingFace.TextCompletion;
using Microsoft.SemanticKernel.Connectors.HuggingFace.TextEmbedding;
using Application.Utils;
using System.Text.Json;
using Microsoft.SemanticKernel.Memory;

ApplicationConfig config;

using (StreamReader r = new StreamReader("app_config.json"))
{
    string json = r.ReadToEnd();
    if (string.IsNullOrEmpty(json))
    {
        throw new Exception("unable to read file app_config.json");
    }
    else
    {
        config = JsonSerializer.Deserialize<ApplicationConfig>(json);
    }

    if (config == null)
    {
        throw new JsonException("Unable to parse application configuration.");
    }
}

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("System", LogLevel.Warning)
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();
// QdrantMemoryStore memoryStore = new QdrantMemoryStore(Env.Var("QDRANT_ENDPOINT"), qdrantPort, vectorSize: 1536, logger);
var memoryStore = new VolatileMemoryStore();

IKernel kernel = new KernelBuilder()
    .WithLogger(logger)
    .Configure(c =>
    {
        c.AddAzureOpenAITextCompletionService(serviceId: "davinci", deploymentName: "text-davinci-003", endpoint: config.CompletionEndpoint, apiKey: config.CompletionApiKey);
        c.AddTextEmbeddingGenerationService("hf-text-embedding", (kernel) => new HFEmbeddingInferenceEndpoint(endpoint: new Uri(config.EmbeddingEndpoint), apiKey: config.EmbeddingApiKey));
    })
    .WithMemoryStorage(memoryStore)
    .Build();

string memoryCollectionName = "cats";

Console.WriteLine("== Adding Memories ==");

await kernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat1", text: "british short hair");
await kernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat2", text: "orange tabby");
await kernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat3", text: "norwegian forest cat");

Console.WriteLine("== Printing Collections in DB ==");

var collections = memoryStore.GetCollectionsAsync();
await foreach (var collection in collections)
{
    Console.WriteLine(collection);
}

Console.WriteLine("== Similarity Searching Memories: My favorite color is orange ==");
var searchResults = kernel.Memory.SearchAsync(memoryCollectionName, "My favorite color is orange", limit: 3, minRelevanceScore: 0.25);

await foreach (var item in searchResults)
{
    Console.WriteLine(item.Metadata.Text + " : " + item.Relevance);
}