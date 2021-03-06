//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace SDKTemplate
{
    public sealed partial class Scenario02_GetStream : Page
    {
        MainPage rootPage = MainPage.Current;

        private HttpClient httpClient;
        private CancellationTokenSource cts;

        public Scenario02_GetStream()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            httpClient = Helpers.CreateHttpClientWithPlugIn();
            cts = new CancellationTokenSource();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();
            httpClient.Dispose();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            // The value of 'AddressField' is set by the user and is therefore untrusted input. If we can't create a
            // valid, absolute URI, we'll notify the user about the incorrect input.
            Uri resourceUri = Helpers.TryParseHttpUri(AddressField.Text);
            if (resourceUri == null)
            {
                rootPage.NotifyUser("Invalid URI.", NotifyType.ErrorMessage);
                return;
            }

            Helpers.ScenarioStarted(StartButton, CancelButton, OutputField);
            rootPage.NotifyUser("In progress", NotifyType.StatusMessage);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, resourceUri);

            // This sample uses a "try" in order to support TaskCanceledException.
            // If you don't need to support cancellation, then the "try" is not needed.
            try
            {
                // Do not buffer the response.
                HttpRequestResult result = await httpClient.TrySendRequestAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead).AsTask(cts.Token);

                if (result.Succeeded)
                {
                    OutputField.Text += Helpers.SerializeHeaders(result.ResponseMessage);

                    StringBuilder responseBody = new StringBuilder();
                    using (Stream responseStream = (await result.ResponseMessage.Content.ReadAsInputStreamAsync()).AsStreamForRead())
                    {
                        int read = 0;
                        byte[] responseBytes = new byte[1000];
                        do
                        {
                            read = await responseStream.ReadAsync(responseBytes, 0, responseBytes.Length);

                            responseBody.AppendFormat("Bytes read from stream: {0}", read);
                            responseBody.AppendLine();

                            // Use the buffer contents for something. We can't safely display it as a string though, since encodings
                            // like UTF-8 and UTF-16 have a variable number of bytes per character and so the last bytes in the buffer
                            // may not contain a whole character. Instead, we'll convert the bytes to hex and display the result.
                            IBuffer responseBuffer = CryptographicBuffer.CreateFromByteArray(responseBytes);
                            responseBuffer.Length = (uint)read;
                            responseBody.AppendFormat(CryptographicBuffer.EncodeToHexString(responseBuffer));
                            responseBody.AppendLine();
                        } while (read != 0);
                    }
                    OutputField.Text += responseBody.ToString();

                    rootPage.NotifyUser("Completed", NotifyType.StatusMessage);
                }
                else
                {
                    Helpers.DisplayWebError(rootPage, result.ExtendedError);
                }
            }
            catch (TaskCanceledException)
            {
                rootPage.NotifyUser("Request canceled.", NotifyType.ErrorMessage);
            }

            Helpers.ScenarioCompleted(StartButton, CancelButton);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource.
            cts = new CancellationTokenSource();
        }
    }
}
