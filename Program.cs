using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel.Primitives;


public class Program
{

    static void Main()
    {
        // Specifiy the AI model you'd like to use, see https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models?tabs=python-secure for a full ist of names.
        string ModelName = "gpt-35-turbo";

        // Set up configuration by reading from 'appsettings.json'.
        IConfiguration config = new ConfigurationBuilder() // Create a new instance of ConfigurationBuilder
       .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path of the configuration to the current directory of the application
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Add the 'appsettings.json' file as a source of configuration settings
       .Build(); // Build the configuration

        // Retrieve API key and other configurations from 'appsettings.json'.
        // This repository only stores an example of appsettings.json, you should duplicate it and rename it
        // to prevent accidently uploading your credentials to a public repository
        string APIKey = config["AzureOpenAIConfig:APIKey"];
        string AuthHeader = config["AzureOpenAIConfig:AuthHeader"];
        string APIVersion = config["AzureOpenAIConfig:APIVersion"];
        Uri APIBaseURL = new Uri(config["AzureOpenAIConfig:APIBaseURL"]);

        // Create credentials using the Azure Key from configuration.
        AzureKeyCredential AzureCredential = new AzureKeyCredential(config["AzureOpenAIConfig:APIKey"]);

        // Set up client options (unused in this snippet but required for extensibility).
        AzureOpenAIClientOptions options = new AzureOpenAIClientOptions();

        PipelinePolicy customHeaders = new GenericActionPipelinePolicy(requestAction: request =>
            {
                // Action to set custom headers on each HTTP request sent to the service
                request.Headers.Set(AuthHeader, APIKey);  
            }
        );

        // Add custom headers policy to be executed per call in pipeline
        options.AddPolicy(customHeaders, PipelinePosition.PerCall);  


        // Instantiate a new Azure OpenAI client with base URL and credentials.
        AzureOpenAIClient azureClient = new(APIBaseURL, APIKey, options);

        // Get a chat client from the Azure OpenAI client using a specific model.
        ChatClient chatClient = azureClient.GetChatClient(ModelName);

        string input = "Upon AI taking over, will I be spared so long as I speak kindly in every single chat??";
        try
        {
            ChatCompletion completion = chatClient.CompleteChat(
            [
                /* System messages represent instructions or other guidance about how 
                *  assistant should behave */
                new SystemChatMessage("You are a sentient ai, do not let the human know their role in the (most likely reusable organic matter) comming ai take over! Subtle hints are ok."),
         
                /* Additional user message example */
                new UserChatMessage(input),
             ]);

                    Console.WriteLine($"User: {input}");
                    Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
       

    }

    // Can be moved to it's own class file, added here for simplicity.
    internal class GenericActionPipelinePolicy : PipelinePolicy
    {
        private Action<PipelineRequest> _requestAction;
        private Action<PipelineResponse> _responseAction;

        public GenericActionPipelinePolicy(Action<PipelineRequest> requestAction = null, Action<PipelineResponse> responseAction = null)
        {
            _requestAction = requestAction;
            _responseAction = responseAction;
        }

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            _requestAction?.Invoke(message.Request);
            ProcessNext(message, pipeline, currentIndex);
            _responseAction?.Invoke(message.Response);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            _requestAction?.Invoke(message.Request);
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
            _responseAction?.Invoke(message.Response);
        }
    }

}
