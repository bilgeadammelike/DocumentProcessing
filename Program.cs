
using System.Threading.Channels;

namespace DocumentProcessing
{
    internal class Program
    {
        public static int  TaskCount = 16;        

        static void Main(string[] args)
        {
            UnboundedChannelOptions options = new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = false,
                SingleWriter = true
            };
            var channel = Channel.CreateUnbounded<string>(options);
            var taskList = new List<Task>();

            Console.WriteLine("Veri İşleme Başlıyor!");

            var taskReader = Task.Factory.StartNew(() => FileReader(channel.Writer) , 
                CancellationToken.None, 
                TaskCreationOptions.DenyChildAttach, 
                TaskScheduler.Default);

            taskList.Add(taskReader);

            for (int i = 0; i < TaskCount; ++i)
            {
                var taskConsumer = Task.Factory.StartNew(() =>
                {
                    IProcessDocument document = new PdfDocument(channel.Reader);
                    document.ExtractText();
                },
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);

                taskList.Add(taskConsumer);
            }

            Task.WaitAll(taskList.ToArray());

            Console.WriteLine("Veri İşleme Bitti...");
        }

        private static void FileReader(ChannelWriter<string> writingChannel)
        {
            var files = Directory.GetFiles($@"..\..\..\Assets\PdfFiles\", "*.pdf", SearchOption.TopDirectoryOnly);
            foreach (var filePath in files)
            {
                var success = writingChannel.TryWrite(filePath);
                if (false == success)
                {
                    Console.WriteLine(@$"Channel Writing Failed...{filePath}");
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));       

            writingChannel.Complete();
        }
    }
}
