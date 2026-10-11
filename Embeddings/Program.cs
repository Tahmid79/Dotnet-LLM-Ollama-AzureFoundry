using System.ClientModel;
using System.Numerics.Tensors;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var endpoint = "https://makh0002-4100-resource.services.ai.azure.com"; // /openai/v1/responses";
var apiKey = config["AzureOpenApiKey:Token"];
var deploymentName = "gpt-5-mini-1";
var embeddingDeploymentName = "text-embedding-3-small";

var credential = new ApiKeyCredential(apiKey);
var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);

IChatClient client = azureClient.GetChatClient(deploymentName).AsIChatClient();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    azureClient.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator();
    
// var embedding = await embeddingGenerator.GenerateVectorAsync("Hello World!");
// Console.WriteLine($"Embedding dimensions: {embedding.Span.Length}");
// foreach (var value in embedding.Span)
// {
//     Console.Write("{0:0.00}", value);
// }

// Compare multiple embeddings using cosine similarity
var catVector = await embeddingGenerator.GenerateVectorAsync("cat");
var dogVector = await embeddingGenerator.GenerateVectorAsync("dog");
var kittenVector = await embeddingGenerator.GenerateVectorAsync("kitten");

Console.WriteLine($"cat-dog similarity {TensorPrimitives.CosineSimilarity(catVector.Span, dogVector.Span)}");
Console.WriteLine($"cat-kitten similarity {TensorPrimitives.CosineSimilarity(catVector.Span, kittenVector.Span)}");
Console.WriteLine($"dog-kitten similarity {TensorPrimitives.CosineSimilarity(dogVector.Span, kittenVector.Span)}");