﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ouroboros.Documents;
using Ouroboros.Documents.Extensions;
using Ouroboros.OpenAI;

[assembly: InternalsVisibleTo("Ouroboros.Test")]

namespace Ouroboros;

// TODO: combine OpenAiClient and Client into a single class.
// TODO: But also keep OpenAiClient abstracted out as a possible way to 
// TODO: Add other LLMs.

public class Client
{
    private readonly OpenAiClient CompletionClient;

    public async Task<string> Complete(string text)
    {
        return await CompletionClient.Complete(text);
    }

    public Document CreateDocument(string text)
    {
        return new Document(this, text); // TODO: return an IDocument builder interface
    }

    public async Task<string> Resolve(string path)
    {
        var text = await System.IO.File.ReadAllTextAsync(path);

        var fragment = new Document(this, text);
        await fragment.Resolve();

        return fragment.ToString();
    }

    /// <summary>
    /// Resolves the next element, and then stops. 
    /// </summary>
    public async Task<Document> ResolveNext(string path)
    {
        var text = await System.IO.File.ReadAllTextAsync(path);

        var doc = new Document(this, text);
        
        await doc.Resolve(new ResolveOptions()
        {
            HaltAfterFirstComplete = true
        });

        return (Document) doc;
    }

    /// <summary>
    /// Resolves the next element, and then stops. 
    /// </summary>
    public async Task ResolveNext(Document document)
    {
        await document.ResolveNext();
    }

    public async Task<string> Summarize(string text, int maxSentences)
    {
        var fragment = new Document(this, 
            $"This is a Harvard business professor who summarizes the provided text into at most {maxSentences} sentences, " + 
            $"solving any spelling and grammatical issues. She preserves the original author's intent and does not censor criticism or add any new meaning." +
            $"If the text involves details that might be attributable to the author, the professor will remove those to protect the author." +
            "The result is professional and succinct, and cannot be traced to the original author in any way.\n\n" + 
            $"Text: {text}\n" +
            $"Summary:");

        await fragment.ResolveAndSubmit();
        var response = fragment.GetLastAsText();

        return response;
    }

    //public async Task<List<string>> Mine(string path)
    //{
    //    var text = await System.IO.File.ReadAllTextAsync(path);
    //    var client = new OpenAiClient(ApiKey);

    //    var sifter = new Sifter(client);

    //    return await sifter.Mine(text);
    //}

    public Client(string apiKey)
    {
        CompletionClient = new OpenAiClient(apiKey);
    }
}