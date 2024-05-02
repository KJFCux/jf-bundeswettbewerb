using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BWB_Auswertung.IO
{
    public class PDF
    {
        public async Task<bool> ConvertHtmlFileToPdf(string html, string outputPdfPath)
        {
            try
            {
                await new BrowserFetcher().DownloadAsync();
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();
                await page.SetContentAsync(html);
                await page.WaitForNetworkIdleAsync();
                await page.PdfAsync(outputPdfPath);
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;

            }
        }

        public void MergePdfFiles(List<string> sourceFiles, string destinationFile, bool deleteSource = false)
        {
            try
            {
                using (PdfWriter writer = new PdfWriter(destinationFile))
                {
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        PdfMerger merger = new PdfMerger(pdf);

                        foreach (string sourceFile in sourceFiles)
                        {
                            using (PdfDocument sourcePdf = new PdfDocument(new PdfReader(sourceFile)))
                            {
                                merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                            }

                            //Ursprungsdateien nach dem Mergen löschen
                            if (deleteSource)
                            {
                                File.Delete(sourceFile);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

    }

}
