using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using ChatApp_RAG_Qdrant.Web.Components;
using ChatApp_RAG_Qdrant.Web.Services;
using ChatApp_RAG_Qdrant.Web.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Same Azure setup as ChatApp_RAG (direct client, no Aspire provisioning).
// Token comes from user secrets: dotnet user-secrets set AzureOpenApiKey:Token <value>
IConfigurationRoot userSecrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var endpoint = "https://makh0002-4100-resource.services.ai.azure.com";
var apiKey = userSecrets["AzureOpenApiKey:Token"]
    ?? builder.Configuration["AzureOpenApiKey:Token"]
    ?? throw new InvalidOperationException(
        "Missing configuration: AzureOpenApiKey:Token. Set it with 'dotnet user-secrets set AzureOpenApiKey:Token <value>'.");
var embeddingDeploymentName = "text-embedding-3-small";
var deploymentName = "gpt-5-mini-1";

var credential = new ApiKeyCredential(apiKey);
var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);

IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    azureClient.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator();

builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

// Qdrant vector store. Works both under Aspire AppHost (ConnectionStrings:vectordb
// injected) and standalone (falls back to localhost:6333).
var vectordbConnectionString = builder.Configuration.GetConnectionString("vectordb");
builder.Services.AddSingleton<QdrantClient>(_ =>
    !string.IsNullOrWhiteSpace(vectordbConnectionString)
        ? new QdrantClient(new Uri(vectordbConnectionString))
        : new QdrantClient("localhost", 6333));
builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton("ingestion_directory",
    new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();