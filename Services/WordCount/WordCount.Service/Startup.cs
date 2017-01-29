// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System.Web.Http;
using Microsoft.ServiceFabric.Data;
using Owin;
using WordCount.Common;

namespace WordCount.Service
{
    /// <summary>
    /// OWIN configuration
    /// </summary>
    public class Startup : IOwinAppBuilder
    {
        private readonly IReliableStateManager _stateManager;

        public Startup(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        /// <summary>
        /// Configures the app builder using Web API.
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(IAppBuilder appBuilder)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 256;

            var config = new HttpConfiguration();

            FormatterConfig.ConfigureFormatters(config.Formatters);
            UnityConfig.RegisterComponents(config, _stateManager);

            config.MapHttpAttributeRoutes();

            appBuilder.UseWebApi(config);
        }
    }
}