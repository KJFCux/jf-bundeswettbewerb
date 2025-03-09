using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using LagerInsights.Properties;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace LagerInsights.IO;

public class PDF
{
    public async Task<bool> ConvertHtmlFileToPdf(string html, string outputPdfPath, bool metadata = false,
        string titel = "Von Auswertung generiertes Dokument", string subject = "Von Auswertung generiertes Dokument",
        string author = "Auswertung")
    {
        try
        {
            await new BrowserFetcher().DownloadAsync();
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            await page.WaitForNetworkIdleAsync();
            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                Landscape = false
            };
            await page.PdfAsync(outputPdfPath, pdfOptions);
            if (metadata) SetPdfMetadata(outputPdfPath, titel, subject, author);
            return true;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            return false;
        }
    }

    public void MergePdfFiles(List<string> sourceFiles, string destinationFile, bool deleteSource = false,
        string titel = "Von Auswertung generiertes Dokument", string subject = "Von Auswertung generiertes Dokument",
        string author = "Auswertung")
    {
        try
        {
            using (var writer = new PdfWriter(destinationFile))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    var merger = new PdfMerger(pdf);

                    foreach (var sourceFile in sourceFiles)
                    {
                        using (var sourcePdf = new PdfDocument(new PdfReader(sourceFile)))
                        {
                            merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                        }

                        //Ursprungsdateien nach dem Mergen löschen
                        if (deleteSource) File.Delete(sourceFile);
                    }
                }
            }

            SetPdfMetadata(destinationFile, titel, subject, author);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void SetPdfMetadata(string pdfPath, string title, string subject, string author)
    {
        try
        {
            using (var pdfReader = new PdfReader(pdfPath))
            using (var pdfWriter = new PdfWriter(pdfPath + "_temp"))
            using (var pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            {
                var documentInfo = pdfDocument.GetDocumentInfo();
                documentInfo.SetTitle(title);
                documentInfo.SetSubject(subject);
                documentInfo.SetAuthor(author);
                documentInfo.SetCreator(Settings.Default.FriendlyName);


                pdfDocument.Close();
            }

            // Replace original file with updated file
            File.Delete(pdfPath);
            File.Move(pdfPath + "_temp", pdfPath);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }
}