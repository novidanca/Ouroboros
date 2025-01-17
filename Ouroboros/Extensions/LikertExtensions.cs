﻿using Ouroboros.Scales;

namespace Ouroboros.Extensions;

public static class LikertAgreementExtensions
{
    /// <summary>
    /// Given a string containing a likert response, this returns a likert enum.
    /// </summary>
    public static LikertAgreement4 ToAgreement4(this string @this)
    {
        var content = @this
            .Trim()
            .ToLower();

        return content switch
        {
            "strongly disagree" => LikertAgreement4.StronglyDisagree,
            "disagree" => LikertAgreement4.Disagree,
            "agree" => LikertAgreement4.Agree,
            "strongly agree" => LikertAgreement4.StronglyAgree,
            _ => LikertAgreement4.NoMatch
        };
    }
}