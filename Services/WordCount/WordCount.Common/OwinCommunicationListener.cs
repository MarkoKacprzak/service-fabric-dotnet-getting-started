// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Fabric;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace WordCount.Common
{
   
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly ServiceContext _serviceContext;

       
        private readonly IOwinAppBuilder _startup;
        private readonly string _appRoot;
        private string _publishAddress;
        private string _listeningAddress;
        /// <summary>
        /// OWIN server handle.
        /// </summary>
        private IDisposable _serverHandle;

        public OwinCommunicationListener(IOwinAppBuilder startup, ServiceContext serviceContext)
            : this(null, startup, serviceContext)
        {
        }

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startup, ServiceContext serviceContext)
        {
            this._startup = startup;
            this._appRoot = appRoot;
            this._serviceContext = serviceContext;
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine("Initialize");

            var serviceEndpoint = this._serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            var port = serviceEndpoint.Port;

            if (this._serviceContext is StatefulServiceContext)
            {
                StatefulServiceContext statefulInitParams = (StatefulServiceContext) this._serviceContext;

                this._listeningAddress =
                    $"http://+:{port}/{statefulInitParams.PartitionId}/{statefulInitParams.ReplicaId}/{Guid.NewGuid()}/";
            }
            else if (this._serviceContext is StatelessServiceContext)
            {
                this._listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(this._appRoot)
                        ? ""
                        : this._appRoot.TrimEnd('/') + '/');
            }
            else
            {
                throw new InvalidOperationException();
            }

            this._publishAddress = this._listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            Trace.WriteLine($"Opening on {this._publishAddress}");

            try
            {
                Trace.WriteLine($"Starting web server on {this._listeningAddress}");

                this._serverHandle = WebApp.Start(this._listeningAddress, appBuilder => this._startup.Configuration(appBuilder));

                return Task.FromResult(this._publishAddress);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine("Close");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            Trace.WriteLine("Abort");

            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this._serverHandle != null)
            {
                try
                {
                    this._serverHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}