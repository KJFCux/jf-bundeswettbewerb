using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace BWB_Auswertung.IO
{
    public class PDF : IAsyncDisposable
    {
        private IBrowser _browser;

        private async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            var fetcher = new BrowserFetcher();
            var installed = fetcher.GetInstalledBrowsers().FirstOrDefault()
                            ?? await fetcher.DownloadAsync();

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = installed.GetExecutablePath()
            });
            return _browser;
        }

        public async Task<bool> ConvertHtmlFileToPdf(string html, string outputPdfPath, bool metadata = false, string titel = "Von Auswertung generiertes Dokument", string subject = "Von Auswertung generiertes Dokument", string author = "Auswertung")
        {
            try
            {
                var browser = await GetBrowserAsync();
                using var page = await browser.NewPageAsync();
                await page.SetContentAsync(html);
                await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { IdleTime = 100 });
                var pdfOptions = new PdfOptions
                {
                    Format = PaperFormat.A4,
                    Landscape = false
                };
                await page.PdfAsync(outputPdfPath, pdfOptions);
                if (metadata)
                {
                    SetPdfMetadata(outputPdfPath, titel, subject, author);
                }
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
            {
                try
                {
                    await _browser.CloseAsync();
                }
                catch (Exception ex)
                {
                    LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Warning);
                }
                finally
                {
                    await _browser.DisposeAsync();
                    _browser = null;
                }
            }
        }

        public void MergePdfFiles(List<string> sourceFiles, string destinationFile, bool deleteSource = false, string titel = "Von Auswertung generiertes Dokument", string subject = "Von Auswertung generiertes Dokument", string author = "Auswertung")
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
                SetPdfMetadata(destinationFile, titel, subject, author);

            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
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
                    documentInfo.SetCreator(BWB_Auswertung.Properties.Settings.Default.FriendlyName);


                    pdfDocument.Close();
                }

                // Replace original file with updated file
                System.IO.File.Delete(pdfPath);
                System.IO.File.Move(pdfPath + "_temp", pdfPath);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

    }

}
