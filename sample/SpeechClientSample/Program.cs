// ---------------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// MIT LicensePermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  </copyright>
//  ---------------------------------------------------------------------------------------------------------------------

namespace SpeechClientSample
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CognitiveServicesAuthorization;
    using Microsoft.Bing.Speech;

    /// <summary>
    /// This sample program shows how to use <see cref="SpeechClient"/> APIs to perform speech recognition.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Short phrase mode URL
        /// </summary>
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");

        /// <summary>
        /// The long dictation URL
        /// </summary>
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");

        /// <summary>
        /// A completed task
        /// </summary>
        private static readonly Task CompletedTask = Task.FromResult(true);

        /// <summary>
        /// Cancellation token used to stop sending the audio.
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private static StringBuilder finalResponse;

        /// <summary>
        /// The entry point to this sample program. It validates the input arguments
        /// and sends a speech recognition request using the Microsoft.Bing.Speech APIs.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public static void Main(string[] args)
        {

            // Send a speech recognition request for the audio.
            finalResponse = new StringBuilder();

            string[] files = GetFiles(args[0]);

            var p = new Program();

            foreach (var file in files)
            {
                p.Run(file, "en-us", LongDictationUrl, args[1]).Wait();
                Console.WriteLine("File {0} processed", file);
            }

            string username = Environment.GetEnvironmentVariable("USERNAME", EnvironmentVariableTarget.Process);

            SaveOutput(String.Format(@"C:\\Users\\{0}\\Documents\\testspeechapi\\apitranscript.txt", username), finalResponse);
        }

        /// <summary>
        /// Invoked when the speech client receives a phrase recognition result(s) from the server.
        /// </summary>
        /// <param name="args">The recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        public Task OnRecognitionResult(RecognitionResult args)
        {

            var response = args;

            if (response.Phrases != null)
            {
                finalResponse.Append(response.Phrases[0].DisplayText);
                finalResponse.Append("\n");
            }

            return CompletedTask;
        }

        /// <summary>
        /// Sends a speech recognition request to the speech service
        /// </summary>
        /// <param name="audioFile">The audio file.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <returns>
        /// A task
        /// </returns>
        public async Task Run(string audioFile, string locale, Uri serviceUrl, string subscriptionKey)
        {
            // create the preferences object
            var preferences = new Preferences(locale, serviceUrl, new CognitiveServicesAuthorizationProvider(subscriptionKey));

            // Create a a speech client
            using (var speechClient = new SpeechClient(preferences))
            {

                speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

                // create an audio content and pass it a stream.
                using (var audio = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {
                    var deviceMetadata = new DeviceMetadata(DeviceType.Near, DeviceFamily.Desktop, NetworkType.Ethernet, OsName.Windows, "1607", "Dell", "T3600");
                    var applicationMetadata = new ApplicationMetadata("SampleApp", "1.0.0");
                    var requestMetadata = new RequestMetadata(Guid.NewGuid(), deviceMetadata, applicationMetadata, "SampleAppService");

                    await speechClient.RecognizeAsync(new SpeechInput(audio, requestMetadata), this.cts.Token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Display the list input arguments required by the program.
        /// </summary>
        /// <param name="message">The message.</param>
        private static void DisplayHelp(string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "SpeechClientSample Help";
            }

            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("Arg[0]: Specify an input directory");
            Console.WriteLine("Arg[1]: Specify the subscription key to access the Speech Recognition Service.");
            Console.WriteLine();
            Console.WriteLine("Sign up at https://www.microsoft.com/cognitive-services/ with a client/subscription id to get a client secret key.");
        }

        private static string[] GetFiles(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            return files;
        }

        private static void SaveOutput(string filename, StringBuilder content)
        {
            StreamWriter writer = new StreamWriter(filename);
            writer.Write(content.ToString());
            writer.Close();
        }
    }
}
