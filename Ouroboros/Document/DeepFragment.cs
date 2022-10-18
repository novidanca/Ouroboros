﻿#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ouroboros.Document.Elements;
using Ouroboros.Document.Extensions;
using Ouroboros.Document.Factories;
using Ouroboros.Document.Mutators;
using Ouroboros.OpenAI;

namespace Ouroboros.Document;

internal class DeepFragment
{
    public List<ElementBase> DocElements { get; set; }

    #region Public API
    public async Task Resolve(ResolveOptions? options = null)
    {
        options ??= new ResolveOptions();

        // TODO: Any element could contain a resolve element. Maybe we should call them all recursively. 
        var resolveElements = DocElements
            .OfType<ResolveElement>()
            .Where(x => !x.IsResolved)
            .ToList();

        // Iterate through any resolve elements first. Resolve them by calling GPT. 
        foreach (var element in resolveElements)
            await ResolveElement(element);

        // Submit to GPT if necessary.
        if (options.SubmitResultForCompletion)
            await SubmitAndAppend(options);
    }

    /// <summary>
    /// Override that always submits the document to GPT-3. This is not the default behavior.
    /// </summary>
    public async Task<TextElement> ResolveAndSubmit(string newElementName = "")
    {
        await Resolve(new ResolveOptions()
        {
            SubmitResultForCompletion = true,
            NewElementName = newElementName 
        });

        return this.GetLastGeneratedAsElement();
    }

    /// <summary>
    /// Returns the text representation of the document model.
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var element in DocElements)
            builder.Append(element.ToString());

        return builder.ToString();
    }
    #endregion

    #region Resolution

    /// <summary>
    /// Submits the document to the LLM, and then appends the result onto the end.
    /// </summary>
    private async Task SubmitAndAppend(ResolveOptions options)
    {
        var documentText = this.ToString();

        var client = new Gpt3Client();
        var result = await client.Complete(documentText);

        var textElement = new TextElement()
        {
            Id = options.NewElementName,
            IsGenerated = true,
            Content = result
        };

        DocElements.Add(textElement);
    }

    private async Task ResolveElement(ResolveElement element)
    {
        // Create a new fragment to help us handle the resolve tag. We swap out the prompt and
        // cut the document off just before the resolve tag.

        var mutator = new ResolverMutator(this, element);
        var fragment = mutator.MutateToNewFragment();

        //     ███ ███ ███ █┼█ ███ ███ ███ ███ █┼┼█     // 
        //     █▄┼ █▄┼ █┼┼ █┼█ █▄┼ █▄▄ ┼█┼ █┼█ ██▄█     // 
        //     █┼█ █▄▄ ███ ███ █┼█ ▄▄█ ▄█▄ █▄█ █┼██     //

        // This call will resolve the fragment, and resolve tags inside the fragment, recursively.
        // A new element is returned. Note that it belongs to the new fragment, which is just sort of a temporary workspace,
        // 
        var newElement = await fragment.ResolveAndSubmit();

        // Plug that content into our element, and mark it resolved.
        element.GeneratedOutput = newElement.Content;
        element.IsResolved = true;
    }
    #endregion


    internal DeepFragment(List<ElementBase> docElements)
    {
        DocElements = docElements;
    }

    internal DeepFragment(string text) 
    {
        var factory = new DocElementsFactory();
        DocElements = factory.Create(text);
    }
}