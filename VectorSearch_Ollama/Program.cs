using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OllamaSharp;
using VectorSearch;

// IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
//
// var endpoint = "https://makh0002-4100-resource.services.ai.azure.com"; // /openai/v1/responses";
// var apiKey = config["AzureOpenApiKey:Token"];
// var embeddingDeploymentName = "text-embedding-3-small";
//
// var credential = new ApiKeyCredential(apiKey);
// var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaApiClient(new Uri("http://localhost:11434"), "all-minilm");

// create and populate the vector store
var vectorStore = new InMemoryVectorStore();

var moviesStore = vectorStore.GetCollection<int, Movie>("movies");

await moviesStore.EnsureCollectionExistsAsync();

foreach (var movie in MovieData.Movies)
{
    // generate the vector embedding for the movie store
    movie.Vector = await generator.GenerateVectorAsync(movie.Description);
    
    // add the overall movie to the in-memory vector store's movie collection
    await moviesStore.UpsertAsync(movie);
}

//1) Embed the user's query
//2) Vectorized-Search
//3) Returns the results

// generate the vector embedding for the user's prompt
var query = "A science fiction movie about space travel";
var queryEmbeddding = await generator.GenerateVectorAsync(query);

// search the knowledge store based on the user's prompt
var searchResults = moviesStore.SearchAsync(queryEmbeddding, top: 2);

// see the results just so we know what they look like
await foreach (var result in searchResults)
{
    Console.WriteLine($"Title: {result.Record.Title}");
    Console.WriteLine($"Description {result.Record.Description}");
    Console.WriteLine($"Score: {result.Score}");
    Console.WriteLine();
}