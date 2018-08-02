/*************************************************************************************
# Copyright 2018 OSIsoft, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# <http://www.apache.org/licenses/LICENSE-2.0>
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# NOTE: this script was designed using the v1.0
# version of the OMF specification, as outlined here:
# http://omf-docs.osisoft.com/en/v1.0
# For more info, see OMF Developer Companion Guide:
# http://omf-companion-docs.osisoft.com
****************************************************************************************/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace IngressServiceAPI.API
{
    /// <summary>
    /// Client used to send OMF message to the ingress service.
    /// </summary>
    public class IngressClient : IDisposable
    {
        public const string CurrentOMFVersion = "1.0";
        private readonly HttpClient _client;
        private string _producerToken;

        public bool UseCompression { get; set; }

        /// <summary>
        /// Create an IngressClient by passing it the required connection information.
        /// </summary>
        /// <param name="serviceUrl">The HTTP endpoint for the ingress service.</param>
        /// <param name="producerToken">Security token used to authenticate with the service.</param>     
        public IngressClient(string serviceUrl, string producerToken)
        {
            /// <summary>
            /// Deployed PI Connector Relay uses a self-signed certificate to authenticate itself.
            /// The OMF client application is responsible for keeping a list of valid relay certificates for security.
            /// However, because this is a simple example, we use this stub method that validates any certificate.
            /// PLEASE do not do this in production.
            /// </summary>
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) => true;

            _client = new HttpClient();
            _client.BaseAddress = new Uri(serviceUrl);

            _producerToken = producerToken;
        }

        /// <summary>
        /// Sends a collection of JSONSchema strings to ingress service.  The JSONSchema describes
        /// the types used for the data that will be sent.
        /// </summary>
        /// <param name="types">A collection of JSONSchema string.</param>
        public void CreateTypes(IEnumerable<string> types)
        {
            string json = string.Format("[{0}]", string.Join(",", types));
            var bytes = Encoding.UTF8.GetBytes(json);
            SendMessageAsync(bytes, MessageType.Type, MessageAction.Create).Wait();
        }

        /// <summary>
        /// Sends a collection of ContainerId, TypeId pairs to the endpoint.
        /// </summary>
        /// <param name="streams"></param>
        public void CreateContainers(IEnumerable<Container> streams)
        {
            string json = JsonConvert.SerializeObject(streams);
            var bytes = Encoding.UTF8.GetBytes(json);
            SendMessageAsync(bytes, MessageType.Container, MessageAction.Create).Wait();
        }

        /// <summary>
        /// Sends the actual values to the ingress service.  This is async to allow for higher
        /// throughput to the event hub.
        /// </summary>
        /// <param name="values">A collection of values and their associated streams.</param>
        /// <returns></returns>
        public Task SendValuesAsync(IEnumerable<object> values)
        {
            string json = JsonConvert.SerializeObject(values);
            var bytes = Encoding.UTF8.GetBytes(json);
            return SendMessageAsync(bytes, MessageType.Data, MessageAction.Create);
        }

        private async Task SendMessageAsync(byte[] body, MessageType msgType, MessageAction action)
        {
            Message msg = new Message();
            msg.ProducerToken = _producerToken;
            msg.MessageType = msgType;
            msg.Action = action;
            msg.MessageFormat = MessageFormat.JSON;
            msg.Body = body;
            msg.Version = CurrentOMFVersion;

            if (UseCompression)
                msg.Compress(MessageCompression.GZip);

            HttpContent content = HttpContentFromMessage(msg);
            HttpResponseMessage response = await _client.PostAsync("" /* use the base URI */, content);

            Console.WriteLine(" Send " + msgType + " message," + " reponse code is " + response.StatusCode.GetHashCode());

            if ((int)response.StatusCode >= 400)
            {
                string error = await response.Content.ReadAsStringAsync();
                Console.WriteLine(error);
            }
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Deployed Connector Relays use a self-signed certificate to authenticate themselves.
        /// The OMF client application is responsible for keeping a list of valid relay certificates for security.
        /// However, because this is a simple example, we use this stub method that validates any certificate.
        /// PLEASE do not do this in production.
        /// </summary>
        public bool ValidateRemoteCertificateIsRelayCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private HttpContent HttpContentFromMessage(Message msg)
        {
            ByteArrayContent content = new ByteArrayContent(msg.Body);
            foreach(var header in msg.Headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }
            return content;
        }

        #region IDisposable
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
