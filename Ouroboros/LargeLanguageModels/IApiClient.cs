﻿using System.Threading.Tasks;

namespace Ouroboros.ApiClients;

/// <summary>
/// Abstracts APIs such as OpenAI so we can easily swap these out.
/// </summary>
internal interface IApiClient
{
    Task<string> Complete(string text);
}