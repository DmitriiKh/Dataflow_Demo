using NAudio.Utils;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks.Dataflow;

namespace Dataflow_Console
{
    internal static class NAudio_example
    {
        internal static void Run() 
        {
            // Define blocks
            var load = new TransformBlock<string, Stream>(async uri =>
            {
                Console.WriteLine($"Loading from {uri}...");

                var response = await new HttpClient().GetAsync(uri);
                return await response.Content.ReadAsStreamAsync();
            });

            var read = new TransformBlock<Stream, WaveStream>(async stream =>
            {
                Console.WriteLine("Reading audio");

                return new WaveFileReader(stream);
            });

            var broadcast = new BroadcastBlock<WaveStream>( waveStream =>
            {
                Console.WriteLine("Broadcasting audio");

                var memoryStream = new MemoryStream();
                using (WaveFileWriter waveFileWriter = new WaveFileWriter(new IgnoreDisposeStream(memoryStream), waveStream.WaveFormat))
                {
                    byte[] bytes = new byte[waveStream.Length];
                    waveStream.Position = 0;
                    waveStream.Read(bytes, 0, (int)waveStream.Length);
                    waveFileWriter.Write(bytes, 0, bytes.Length);
                }
               
                waveStream.Position = 0;
                memoryStream.Position = 0;
                return new WaveFileReader(memoryStream);
            });

            var play = new ActionBlock<WaveStream>(async waveStream =>
            {
                Console.WriteLine("Playing audio");

                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(waveStream);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            });

            var waveform = new TransformBlock<WaveStream, Image>(async waveStream =>
            {
                Console.WriteLine("Creating a waveform image");

                var settings = new StandardWaveFormRendererSettings
                {
                    Width = 640,
                    TopHeight = 32,
                    BottomHeight = 32
                };
                var renderer = new WaveFormRenderer();
                return renderer.Render(waveStream, settings);
            });

            var saveImage = new TransformBlock<Image, string>(async image =>
            {
                Console.WriteLine("Saving an image");

                var fileName = "waveform.png";
                image.Save(fileName);
                return fileName;
            });

            var openFile = new ActionBlock<string>(async fileName =>
            {
                Console.WriteLine("Opening an image");

                var processInfo = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = true
                };
                Process.Start(processInfo);
            });

            // Link blocks in to a pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            load.LinkTo(read, linkOptions);
            read.LinkTo(broadcast, linkOptions);
            broadcast.LinkTo(play, linkOptions);
            broadcast.LinkTo(waveform, linkOptions);
            waveform.LinkTo(saveImage, linkOptions);
            saveImage.LinkTo(openFile, linkOptions);

            // Push data to pipeline and start it
            load.Post("http://programmerincanada.com/wp-content/uploads/2023/10/sin_mono.wav");
            load.Complete();

            // Wait for completion
            play.Completion.Wait();
            openFile.Completion.Wait();

            Console.WriteLine("Done! Press any key");
            Console.ReadKey();
        }
    }
}
