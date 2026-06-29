using Microsoft.Extensions.Logging;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Osmos.Business.Worker.Threads
{
    public static class HotTokensThread
    {
        public static void Start(
            TokensManager tokensManager,
            HotTokensManager hotTokensManager
            )
        {
            var hotTokensLoop = new Thread(new ThreadStart(() => HotTokensLoop(tokensManager, hotTokensManager)));

            hotTokensLoop.Start();
        }

        private static Logger _logger = new Logger("HotTokensThread", true);

        private static async void HotTokensLoop(
            TokensManager tokensManager,
            HotTokensManager hotTokensManager)
        {
            do
            {
                try
                {
                    var hotTokensResut = await tokensManager.GetHotTokensAsync();

                    var hotTokens = new List<HotToken>();
                    int i = 1;
                    foreach (var item in hotTokensResut.OrderByDescending(_ => _.Score))
                    {
                        var hotToken = new HotToken
                        {
                            Id = i++.ToString(),
                            Score = item.Score,
                            Token = item.Token
                        };

                        hotTokens.Add(hotToken);
                    }

                    //await hotTokensManager.CreateManyAsync(hotTokens.ToArray());

                    await hotTokensManager.BlukUpdateManyAsync(hotTokens);
                }
                catch (Exception e) {
                    ;
                }

                Thread.Sleep(6000);
            } while (true);
        }
    }
}
