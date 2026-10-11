using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// dotnet user-secrets set AzureOpenApiKey:Token
var endpoint = "https://makh0002-4100-resource.services.ai.azure.com"; // /openai/v1/responses";
var apiKey = config["AzureOpenApiKey:Token"];
var deploymentName = "gpt-5-mini-1";

var credential = new ApiKeyCredential(apiKey);
var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
IChatClient client = azureClient.GetChatClient(deploymentName).AsIChatClient();

foreach (var imagePath in Directory.GetFiles("Images", "*.jpg"))
{
    var name = Path.GetFileNameWithoutExtension(imagePath);
    
    var message = new ChatMessage(ChatRole.User, $$"""
                                                   Extract information from this image from camera {{name}}.

                                                       Respond with a JSON object in this form: {
                                                       "Status": string // One of these values: "Clear", "Flowing", "Congested", "Blocked",
                                                       "NumCars": number,
                                                       "NumTrucks": number
                                                   }
                                                   """);
    
    message.Contents.Add(new DataContent(File.ReadAllBytes(imagePath), "image/jpeg"));
    
    var response = await client.GetResponseAsync<TrafficCamResult>([message]);
    
    if(response.TryGetResult(out var result))
    {
        Console.WriteLine($"{name} status: {result.Status} (cars: {result.NumCars} , trucks: {result.NumTrucks})");
    }
    
}

class TrafficCamResult
{
    public TrafficCamStatus Status { get; set; }
    public int NumCars { get; set; }
    public int NumTrucks { get; set; }
    public enum TrafficCamStatus { Clear, Flowing, Congested, Blocked }
}