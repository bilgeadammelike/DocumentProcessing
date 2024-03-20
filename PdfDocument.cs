using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System.Threading.Channels;

namespace DocumentProcessing
{
    public class PdfDocument : IProcessDocument
    {
        public ChannelReader<string> ReaderChannel { get; }

        public PdfDocument(ChannelReader<string> readerChannel)
        {
            ReaderChannel = readerChannel;
        }       

        public void ExtractText()
        {

            while (true)
            {
                try
                {
                    string? filePath = string.Empty;

                    if (ReaderChannel.Completion.IsCompleted)
                    {
                        break;
                    }

                    bool success = ReaderChannel.TryRead(out filePath);
                    if(false == success || string.IsNullOrEmpty(filePath))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        continue;
                    }
                    
                    //Get stream from an existing PDF document. 
                    FileStream docStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read);

                    //Load the PDF document.
                    PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);

                    //Loading page collections.
                    PdfLoadedPageCollection loadedPages = loadedDocument.Pages;

                    string extractedText = string.Empty;

                    //Extract text from existing PDF document pages.
                    foreach (PdfLoadedPage loadedPage in loadedPages)
                    {
                        extractedText += loadedPage.ExtractText();
                    }

                    //Close the document.
                    loadedDocument.Close(true);
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                catch(Syncfusion.Pdf.PdfInvalidPasswordException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }           
        }
    }
}
