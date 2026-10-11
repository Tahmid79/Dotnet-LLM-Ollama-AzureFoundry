using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI;
using ChatApp_RAG.Components;
using ChatApp_RAG.Services;
using ChatApp_RAG.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureOpenAI:Endpoint https://YOUR-DEPLOYMENT-NAME.openai.azure.com


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var endpoint = "https://makh0002-4100-resource.services.ai.azure.com"; // /openai/v1/responses";
var apiKey = config["AzureOpenApiKey:Token"];
var embeddingDeploymentName = "text-embedding-3-small";
var deploymentName = "gpt-5-mini-1";

var credential = new ApiKeyCredential(apiKey);
var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);



// var azureOpenAIEndpoint =
//     new Uri(
//         new Uri(builder.Configuration["AzureOpenAI:Endpoint"] ??
//                 throw new InvalidOperationException(
//                     "Missing configuration: AzureOpenAi:Endpoint. See the README for details.")), "/openai/v1");
// #pragma warning disable OPENAI001 // OpenAIClient(AuthenticationPolicy, OpenAIClientOptions) and GetResponsesClient() are experimental and subject to change or removal in future updates.
// var azureOpenAi = new OpenAIClient(
//     new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
//     new OpenAIClientOptions { Endpoint = azureOpenAIEndpoint });

IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

// var chatClient = azureOpenAi.GetResponsesClient().AsIChatClient("gpt-4o-mini");
#pragma warning restore OPENAI001

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    azureClient.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator();

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
builder.Services.AddSqliteCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);

builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton("ingestion_directory",
    new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

var app = builder.Build();

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