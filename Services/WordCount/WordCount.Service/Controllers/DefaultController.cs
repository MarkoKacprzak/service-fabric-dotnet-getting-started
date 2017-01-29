// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount.Service.Controllers
{
   /// <summary>
    /// Default controller.
    /// </summary>
    public class DefaultController : ApiController
    {
        private readonly IReliableStateManager _stateManager;

        public DefaultController(IReliableStateManager stateManager)
        {
            this._stateManager = stateManager;
        }

        [HttpGet]
        [Route("Count")]
        public async Task<IHttpActionResult> Count()
        {
            var statsDictionary = await this._stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("statsDictionary");

            using (var tx = this._stateManager.CreateTransaction())
            {
                var result = await statsDictionary.TryGetValueAsync(tx, "Number of Words Processed");

                if (result.HasValue)
                {
                    return this.Ok(result.Value);
                }
            }

            return this.Ok(0);
        }

        [HttpPut]
        [Route("AddWord/{word}")]
        public async Task<IHttpActionResult> AddWord(string word)
        {
            var queue = await this._stateManager.GetOrAddAsync<IReliableQueue<string>>("inputQueue");

            using (var tx = this._stateManager.CreateTransaction())
            {
                await queue.EnqueueAsync(tx, word);

                await tx.CommitAsync();
            }

            return this.Ok();
        }
    }
}