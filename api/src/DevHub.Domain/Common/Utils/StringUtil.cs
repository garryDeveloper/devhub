using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DevHub.Domain.Common.Utils;

public static class StringUtil
{
    public static string ToKebabCase(string value)
    {
        // Replace all non-alphanumeric characters with a dash
        value = Regex.Replace(value, @"[^0-9a-zA-Z]", "-");

        // Replace all subsequent dashes with a single dash
        value = Regex.Replace(value, @"[-]{2,}", "-");

        // Remove any trailing dashes
        value = Regex.Replace(value, @"-+$", string.Empty);

        // Remove any dashes in position zero
        if (value.StartsWith("-")) value = value.Substring(1);

        // Lowercase and return
        return value.ToLower();
    }
}
