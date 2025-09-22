using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cloud9_2.Services
{
    public class MetadataExtractor
    {
        public MetadataExtractor(IConfiguration configuration)
        {
            Console.WriteLine("MetadataExtractor initialized with iText 7.");
        }

        public async Task<(string DocumentType, Dictionary<string, string> Metadata)> ExtractMetadata(string filePath)
        {
            string documentType = "Unknown";
            var metadata = new Dictionary<string, string>();
            var culture = new CultureInfo("hu-HU");

            Console.WriteLine($"Starting extraction for file: {filePath}");
            try
            {
                using var pdfDocument = new PdfDocument(new PdfReader(filePath));
                var text = new StringBuilder();
                var strategy = new SimpleTextExtractionStrategy();
                for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                    text.AppendLine(pageText);
                }
                string contentText = text.ToString();
                Console.WriteLine($"Extracted raw text from {Path.GetFileName(filePath)}: {contentText}");

                if (string.IsNullOrEmpty(contentText))
                {
                    Console.WriteLine($"Warning: No text extracted from {Path.GetFileName(filePath)}. Verify PDF content.");
                    return (documentType, metadata);
                }

                string lowerContent = contentText.ToLower(culture);

                // Document classification
                if (Regex.IsMatch(contentText, @"\b(SZÁMLA|Számlaszám|számla|invoice)\b") ||
                    Regex.IsMatch(lowerContent, @"\b(összesen|netto|brutto|áfá|kelt|teljesítés|fizetési\s+határidő)\b"))
                {
                    documentType = "Invoice";
                }
                else if (Regex.IsMatch(lowerContent, @"\b(szerződés|contract|megállapodás|aláírás|szerződési\s+feltétel)\b"))
                {
                    documentType = "Contract";
                }
                else if (Regex.IsMatch(lowerContent, @"\b(szállítólevél|delivery\s+note|szállítás|átvevő|szállítmány)\b"))
                {
                    documentType = "DeliveryNote";
                }

                Console.WriteLine($"Classified {Path.GetFileName(filePath)} as: {documentType}");

                if (documentType == "Invoice")
                {
                    // Invoice Number
                    var invoiceRegex = new Regex(@"(?:Számlaszám[:\s]*)([A-Za-z0-9/\-]+)", RegexOptions.IgnoreCase);
                    if (invoiceRegex.Match(contentText) is { Success: true } invoiceMatch)
                        metadata["InvoiceNumber"] = invoiceMatch.Groups[1].Value.Trim();

                    // Seller Section
                    var sellerRegex = new Regex(@"(?:Számla\s*kiállító\s*adatai|Számlakibocsátó|Eladó)[:\s\n]*([\s\S]*?)(?=(Vevő|Fizetési|Megnevezés|Nettó|Összesen))", RegexOptions.IgnoreCase);
                    if (sellerRegex.Match(contentText) is { Success: true } sellerMatch)
                    {
                        var sellerText = sellerMatch.Groups[1].Value.Replace("\n", " ").Trim();
                        metadata["SellerRaw"] = sellerText;

                        var nameMatch = Regex.Match(sellerText, @"(.+?(Kft\.|Bt\.|Rt\.))", RegexOptions.IgnoreCase);
                        if (nameMatch.Success) metadata["SellerName"] = nameMatch.Value.Trim();

                        var zipCityMatch = Regex.Match(sellerText, @"(\d{4})\s*([A-Za-zÁÉÍÓÖŐÚÜŰ]+)", RegexOptions.IgnoreCase);
                        if (zipCityMatch.Success) metadata["SellerCity"] = zipCityMatch.Groups[2].Value.Trim();

                        var addressMatch = Regex.Match(sellerText, @"\d{4}\s*[A-Za-zÁÉÍÓÖŐÚÜŰ]+\s*(.+?utca.+|\d+.+)", RegexOptions.IgnoreCase);
                        if (addressMatch.Success) metadata["SellerAddress"] = addressMatch.Value.Trim();

                        var taxMatch = Regex.Match(sellerText, @"Adószám[:\s]*([0-9\-]+)", RegexOptions.IgnoreCase);
                        if (taxMatch.Success) metadata["SellerTaxNumber"] = taxMatch.Groups[1].Value.Trim();

                        var bankMatch = Regex.Match(sellerText, @"Bankszáml(a|aszám)[:\s]*([A-Z0-9\- ]+)", RegexOptions.IgnoreCase);
                        if (bankMatch.Success) metadata["SellerBankAccount"] = bankMatch.Groups[2].Value.Trim();
                    }

                    // Buyer Section
                    var buyerRegex = new Regex(@"(?:Vevő\s*adatai|Vevő)[:\s\n]*([\s\S]*?)(?=(Fizetési|Megnevezés|Nettó|Összesen))", RegexOptions.IgnoreCase);
                    if (buyerRegex.Match(contentText) is { Success: true } buyerMatch)
                    {
                        var buyerText = buyerMatch.Groups[1].Value.Replace("\n", " ").Trim();
                        metadata["BuyerRaw"] = buyerText;

                        var nameMatch = Regex.Match(buyerText, @"(.+?(Kft\.|Bt\.|Rt\.))", RegexOptions.IgnoreCase);
                        if (nameMatch.Success) metadata["BuyerName"] = nameMatch.Value.Trim();

                        var zipCityMatch = Regex.Match(buyerText, @"(\d{4})\s*([A-Za-zÁÉÍÓÖŐÚÜŰ]+)", RegexOptions.IgnoreCase);
                        if (zipCityMatch.Success) metadata["BuyerCity"] = zipCityMatch.Groups[2].Value.Trim();

                        var addressMatch = Regex.Match(buyerText, @"\d{4}\s*[A-Za-zÁÉÍÓÖŐÚÜŰ]+\s*(.+?utca.+|\d+.+)", RegexOptions.IgnoreCase);
                        if (addressMatch.Success) metadata["BuyerAddress"] = addressMatch.Value.Trim();

                        var taxMatch = Regex.Match(buyerText, @"Adószám[:\s]*([0-9\-]+)", RegexOptions.IgnoreCase);
                        if (taxMatch.Success) metadata["BuyerTaxNumber"] = taxMatch.Groups[1].Value.Trim();

                        var postMatch = Regex.Match(buyerText, @"Postázási\s+cím[:\s]*(.+)", RegexOptions.IgnoreCase);
                        if (postMatch.Success) metadata["BuyerPostalAddress"] = postMatch.Groups[1].Value.Trim();
                    }

                    // Payment Method
                    var paymentMethodRegex = new Regex(@"Fizetési\s*mód[:\s]*([A-Za-z0-9 ]+)", RegexOptions.IgnoreCase);
                    if (paymentMethodRegex.Match(contentText) is { Success: true } paymentMatch)
                        metadata["PaymentMethod"] = paymentMatch.Groups[1].Value.Trim();

                    // Dates
                    var dateRegex = new Regex(@"\b(\d{4}\.\d{2}\.\d{2})\b");
                    var dateMatches = dateRegex.Matches(contentText);
                    if (dateMatches.Count > 0)
                    {
                        metadata["PerformanceDate"] = dateMatches[0].Value;
                        if (dateMatches.Count > 1)
                            metadata["InvoiceDate"] = dateMatches[1].Value;
                        if (dateMatches.Count > 2)
                            metadata["DueDate"] = dateMatches[2].Value;
                    }

                    // Totals
                    var totalRegex = new Regex(@"Összesen[:\s]*(\d+(?:\s\d{3})*(?:,\d+)?)\s*(\d+(?:\s\d{3})*(?:,\d+)?)\s*(\d+(?:\s\d{3})*(?:,\d+)?)", RegexOptions.IgnoreCase);
                    if (totalRegex.Match(contentText) is { Success: true } totalMatch)
                    {
                        metadata["NetTotal"] = totalMatch.Groups[1].Value.Replace(" ", "").Replace(",", ".");
                        metadata["VATTotal"] = totalMatch.Groups[2].Value.Replace(" ", "").Replace(",", ".");
                        metadata["GrossTotal"] = totalMatch.Groups[3].Value.Replace(" ", "").Replace(",", ".");
                    }

                    // Gross Total with Ft (take last match)
                    var grossTotalRegex = new Regex(@"\b(\d{1,3}(?:\s\d{3})*\sFt)\b", RegexOptions.IgnoreCase);
                    var grossMatches = grossTotalRegex.Matches(contentText);
                    if (grossMatches.Count > 0)
                        metadata["GrossTotalFt"] = grossMatches[^1].Groups[1].Value.Trim();

                    // Paid Status
                    if (Regex.IsMatch(lowerContent, @"\bfizetve\b"))
                        metadata["PaidStatus"] = "Fizetve";

                    Console.WriteLine($"Metadata extracted for {Path.GetFileName(filePath)}: {string.Join(", ", metadata.Select(m => $"{m.Key}: {m.Value}"))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting metadata from {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return (documentType, metadata);
        }
    }
}
