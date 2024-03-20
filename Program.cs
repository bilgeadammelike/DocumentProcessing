using System.Threading.Channels;

namespace DocumentProcessing
{
    internal class Program
    {
        public static int  TaskCount = 8;    
        
        public static void ReadLicenceKey()
        {
            var license = File.ReadAllText(@$"..\..\..\Assets\SyncFusionLicense.txt");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(license);
        }

        static void Main(string[] args)
        {
            ReadLicenceKey();

            UnboundedChannelOptions options = new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = false,
                SingleWriter = true
            };
            var channel = Channel.CreateUnbounded<string>(options);
            var taskList = new List<Task>();

            Console.WriteLine("Document Processing is Starting...!");

            var taskReader = Task.Factory.StartNew(() => FileReader(channel) , 
                CancellationToken.None, 
                TaskCreationOptions.DenyChildAttach, 
                TaskScheduler.Default);

            taskList.Add(taskReader);

            for (int i = 0; i < TaskCount; ++i)
            {
                var taskConsumer = Task.Factory.StartNew(() =>
                {
                    IProcessDocument document = new WordDocuments(channel.Reader);
                    document.ExtractText();
                },
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);

                taskList.Add(taskConsumer);
            }

            Task.WaitAll(taskList.ToArray());

            Console.WriteLine("Document Processing Finished...");
        }

        private static void FileReader(Channel<string> channel)
        {
            var channelWriter = channel.Writer;
            var channelReader = channel.Reader;

            while (true)
            {
                var files = Directory.GetFiles($@"..\..\..\Assets\", "*.*", SearchOption.AllDirectories);
                foreach (var filePath in files)
                {
                    var success = channelWriter.TryWrite(filePath);
                    if (false == success)
                    {
                        Console.WriteLine(@$"Channel Writing Failed...{filePath}");
                    }
                }

                while (channelReader.Count > files.Length/2)
                {
                    Thread.Sleep(10);
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));

            channelWriter.Complete();
        }
    }
}
