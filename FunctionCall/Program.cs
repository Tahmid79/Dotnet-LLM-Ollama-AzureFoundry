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

var chatOptions = new ChatOptions
{
  Tools = [AIFunctionFactory.Create((string location, string unit) =>
  {
    // Here you would call a weather api to get the weather for the location
    var temperature = Random.Shared.Next(5, 20);
    var condition = Random.Shared.Next(0, 1) == 0 ? "sunny" : "rainy";

    return $"The weather is {temperature} degrees C and conditions {condition}";
  },
    "get_current_weather", 
    "Get the current weather in a given location")]  
};

List<ChatMessage> chatHistory = [new(ChatRole.System, """
                                                      You are a hiking enthusiast who helps people discover fun hikes in their area. 
                                                      You are upbeat and friendly.
                                                      """ )];
                                                      
chatHistory.Add(new(ChatRole.User, """
I live in Istanbul and I am looking for a moderate intensity hike
What's the current weather like?
""" ));

Console.WriteLine($"{chatHistory.Last().Role} >> {chatHistory.Last()}");

ChatResponse response = await client.GetResponseAsync(chatHistory);

chatHistory.Add(new(ChatRole.Assistant, response.Text));

Console.WriteLine($"{chatHistory.Last().Role} >> {chatHistory.Last()}");


