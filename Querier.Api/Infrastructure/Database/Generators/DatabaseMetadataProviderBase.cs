using System;
using System.Linq;

namespace Querier.Api.Infrastructure.Database.Generators;

public abstract class DatabaseMetadataProviderBase
{
    protected string NormalizeCsString(string str)
    {
        string csName = str.Replace("@", "");
        csName = csName.Replace("p_", "");
        csName = csName.Replace("P_", "");
        return ToPascalCase(csName);
    }

    private string ToPascalCase(string str)
    {
        // Replace all non-letter and non-digits with an underscore and lowercase the rest.
        string sample = string.Join("", str?.Select(c => char.IsLetterOrDigit(c) ? c.ToString().ToLower() : "_").ToArray());

        // Split the resulting string by underscore
        // Select first character, uppercase it and concatenate with the rest of the string
        var arr = sample?
            .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => $"{s.Substring(0, 1).ToUpper()}{s.Substring(1)}");

        // Join the resulting collection
        sample = string.Join("", arr);

        return sample;
    }
}