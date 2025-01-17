using System;
using Ouroboros.Chaining.TemplateDialog;
using Ouroboros.Chaining.TemplateDialog.Templates;
using Ouroboros.Endpoints;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using Ouroboros.Chaining;
using Ouroboros.LargeLanguageModels;
using Ouroboros.LargeLanguageModels.ChatCompletions;
using Ouroboros.LargeLanguageModels.Completions;
using Ouroboros.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Tokenizer.GPT3;

[assembly: InternalsVisibleTo("Ouroboros.Test")]

namespace Ouroboros;

public class OuroClient 
{
    private readonly string ApiKey;
    private readonly CompletionRequestHandler CompletionHandler;
    private readonly ChatRequestHandler ChatHandler;

    private OuroModels DefaultCompletionModel = OuroModels.TextDavinciV3;
    private OuroModels DefaultChatModel = OuroModels.Gpt_4;
    private ITemplateEndpoint? TemplateRequestHandler;
    
    /// <summary>
    /// For gaining direct access to a Betalgo client, without going through the OuroClient.
    /// </summary>
    public OpenAIService GetInnerClient => GetClient();

    public Dialog CreateDialog()
    {
        return new Dialog(this);
    }

    #region TemplateDialog & TemplateEndpoints
	    public TemplateDialog CreateTemplateDialog()
	    {
			return new TemplateDialog(this);
	    }

	    /// <summary>
	    /// Set the TemplateEndpoint for this Ouro Client. All calls to 
	    /// </summary>
	    /// <param name="endpoint"></param>
	    public void SetTemplateEndpoint(ITemplateEndpoint endpoint)
	    {
		    TemplateRequestHandler = endpoint;
	    }

	    public async Task<OuroResponseBase> SendTemplateAsync(IOuroTemplateBase templateBase,
		    ITemplateEndpoint? customEndpoint = null)
	    {
		    if (customEndpoint != null)
			    return await customEndpoint.SendTemplateAsync(nameof(templateBase), templateBase);

            if (TemplateRequestHandler != null)
                return await TemplateRequestHandler.SendTemplateAsync(nameof(templateBase), templateBase);

            throw new NotImplementedException("OuroClient does not have a TemplateEndpoint. Either set an endpoint on OuroClient using SetTemplateEndpoint, or provide an ITemplateEndpoint.");
	    }

	    public async Task<OuroResponseBase> SendTemplateAsync(string templateName, IOuroTemplateBase templateBase,
		    ITemplateEndpoint? customEndpoint = null)
	    {
		    if (customEndpoint != null)
			    return await customEndpoint.SendTemplateAsync(templateName, templateBase);

		    if (TemplateRequestHandler != null)
			    return await TemplateRequestHandler.SendTemplateAsync(templateName, templateBase);

		    throw new NotImplementedException("OuroClient does not have a TemplateEndpoint. Either set an endpoint on OuroClient using SetTemplateEndpoint, or provide an ITemplateEndpoint.");
	    }
    #endregion
    

    /// <summary>
    /// Coverts text into tokens. Uses GPT3Tokenizer.
    /// </summary>
    public static List<int> Tokenize(string text)
    {
        var tokens = TokenizerGpt3.Encode(text, cleanUpCREOL: true); // cleanup improves accuracy

        return tokens.ToList();
    }

    /// <summary>
    /// Gets the number of tokens the given text would take up. Uses GPT3Tokenizer.
    /// </summary>
    public static int TokenCount(string text)
    {
        var tokens = Tokenize(text);

        return tokens.Count;
    }

    /// <summary>
    /// Handles a text completion request.
    /// </summary>
    public async Task<OuroResponseBase> CompleteAsync(string prompt, CompleteOptions? options = null)
    {
        options ??= new CompleteOptions();
        options.Model ??= DefaultCompletionModel;
        var api = GetClient();

        return await CompletionHandler.Complete(prompt, api, options);
    }

    /// <summary>
    /// Handles a chat completion request.
    /// </summary>
    public async Task<OuroResponseBase> ChatAsync(List<ChatMessage> messages, ChatOptions? options = null)
    {
        options ??= new ChatOptions();
        options.Model ??= DefaultChatModel;

        var api = GetClient();

        return await ChatHandler.CompleteAsync(messages, api, options);
    }

    /// <summary>
    /// Configures a default model that will be used for all completions initiated from this client,
    /// unless overriden by passing in a model via CompleteOptions.
    /// </summary>
    public void SetDefaultCompletionModel(OuroModels model)
    {
        DefaultCompletionModel = model;
    }

    /// <summary>
    /// Configures a default model that will be used for all completions initiated from this client,
    /// unless overriden by passing in a model via CompleteOptions.
    /// </summary>
    public void SetDefaultChatModel(OuroModels model)
    {
        DefaultChatModel = model;
    }

    private CompleteOptions ConfigureOptions(CompleteOptions? options)
    {
        options ??= new CompleteOptions();
        options.Model ??= DefaultCompletionModel;

        return options;
    }

    private ChatOptions ConfigureOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.Model ??= DefaultChatModel;

        return options;
    }

    internal OpenAIService GetClient()
    {
        return new OpenAIService(new OpenAiOptions
        {
            ApiKey = ApiKey
        });
    }

    public OuroClient(string apiKey, ITemplateEndpoint? customEndpoint = null)
    {
        // Create an empty services collection, which we need downstream for Polly's retry policy.
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        CompletionHandler = new CompletionRequestHandler(serviceProvider);
        ChatHandler = new ChatRequestHandler(serviceProvider);
        ApiKey = apiKey;
        TemplateRequestHandler = customEndpoint;
    }
}