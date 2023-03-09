﻿using System.Threading.Tasks;

namespace Ouroboros.LargeLanguageModels;

/// <summary>
/// Abstracts APIs such as OpenAI so we can easily swap these out.
/// </summary>
internal interface IApiClient
{
    Task<OuroResponseBase> Complete(string prompt, CompleteOptions? options);
}