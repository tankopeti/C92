using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cloud9_2.Helpers
{
public static class CurrencyHelper
{
    // Fallback mapping in case database values are missing
    private static readonly Dictionary<int, (string CurrencyCode, string Locale)> CurrencyMap = new Dictionary<int, (string, string)>
    {
        { 1, ("HUF", "hu-HU") },
        { 2, ("EUR", "de-DE") },
        { 3, ("USD", "en-US") },
        { 4, ("GBP", "en-GB") },
        { 5, ("JPY", "ja-JP") },
        { 6, ("CHF", "de-CH") },
        { 7, ("AUD", "en-AU") },
        { 8, ("CAD", "en-CA") },
        { 9, ("CNY", "zh-CN") },
        { 10, ("PLN", "pl-PL") }
    };

        public static CultureInfo GetCultureInfoForCurrencyId(int currencyId, string currencyLocale = null)
        {
            if (!string.IsNullOrEmpty(currencyLocale))
            {
                try
                {
                    return new CultureInfo(currencyLocale);
                }
                catch (CultureNotFoundException ex)
                {
                    Console.WriteLine($"Invalid locale: {currencyLocale}, falling back. Error: {ex.Message}");
                }
            }
            // Fallback mapping
            var map = new Dictionary<int, string>
            {
                { 1, "hu-HU" }, { 2, "de-DE" }, { 3, "en-US" }, { 4, "en-GB" }, { 5, "ja-JP" },
                { 6, "de-CH" }, { 7, "en-AU" }, { 8, "en-CA" }, { 9, "zh-CN" }, { 10, "pl-PL" }
            };
            return new CultureInfo(map.TryGetValue(currencyId, out var locale) ? locale : "hu-HU");
        }
    }
}