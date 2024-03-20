using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.Diagnostics;
using System.Threading.Channels;

namespace DocumentProcessing
{
    public class WordDocuments : IProcessDocument
    {
        public ChannelReader<string> ReaderChannel { get; }

        public int SuccessCount { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;

        public WordDocuments(ChannelReader<string> readerChannel)
        {
            ReaderChannel = readerChannel;
        }

        public void ExtractText()
        {
            string? filePath = string.Empty;
            Stopwatch sw = Stopwatch.StartNew();
            while (true)
            {
                try
                {                   
                    if (ReaderChannel.Completion.IsCompleted)
                    {
                        break;
                    }

                    bool success = ReaderChannel.TryRead(out filePath);
                    if (false == success || string.IsNullOrEmpty(filePath))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        continue;
                    }
                    sw.Restart();
                    FileStream fileStreamPath = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    //Opens an existing document from file system through constructor of WordDocument class
                    using (WordDocument document = new WordDocument(fileStreamPath, FormatType.Automatic))
                    {
                        //Gets the document text
                        string text = document.GetText();
                        
                        //Closes the Word document
                        document.Close();

                        ++SuccessCount;
                    }
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                catch (Syncfusion.Pdf.PdfInvalidPasswordException ex)
                {
                    ++ErrorCount;
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    ++ErrorCount;
                    //Console.WriteLine($@"{filePath} - {ex.ToString()}");
                }
                finally
                {
                    sw.Stop();
                    Console.WriteLine($@"{filePath}:S:{SuccessCount}, E:{ErrorCount}: Duration:{sw.Elapsed.TotalSeconds} sn");
                }
            }
        }
    }
}
