﻿#nullable enable
namespace Ouroboros.Document;

internal class ResolveOptions
{
    public bool SubmitResultForCompletion { get; set; } = false;
    public string NewElementName { get; set; } = string.Empty; 
}
