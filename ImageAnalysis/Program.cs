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

// user prompts
var promptDescribe = "Describe the image";
var promptAnalyze = "How many red cars are there in the picture and how many other cars are there?";

// prompts
string systemPrompt = "You are a useful assistant that describes images using a direct style";
var userPrompt = promptAnalyze;

List<ChatMessage> messages =
[
    new ChatMessage(ChatRole.System, systemPrompt),
    new ChatMessage(ChatRole.User, userPrompt)
];

// read the image bytes, create a new image content part and add it to the messages
var imageFileName = "cars.png";
string image = Path.Combine(Directory.GetCurrentDirectory(), "images", imageFileName);

AIContent aic = new DataContent(File.ReadAllBytes(image), "image/png");
var message = new ChatMessage(ChatRole.User, [aic]);
messages.Add(message);

// send the messages to the assistant
var response = await client.GetResponseAsync(messages);
Console.WriteLine($"Prompt: {userPrompt}");
Console.WriteLine($"Image: {imageFileName}");
Console.WriteLine($"Response: {response.Messages[0]}");