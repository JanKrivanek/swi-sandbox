using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SolarWinds.UniversalPolling.Client;
using SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic.Protobuf;
using SolarWinds.UniversalPolling.ResultsHandler.Client.ClientApi;

namespace SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic
{
    public class MySampleHwhMonitoringLogic : IMonitoringPlugin
    {
        public bool Executed { get; set; }

        public async Task Execute(IMonitoringFramework monitoringFramework, CancellationToken cancellationToken)
        {
            Executed = true;

            //throw new Exception("ssssssssssssssss");

            var resultHandler = monitoringFramework.GetResultsHandlerClient();

            MyHwhDataPoint data = new MyHwhDataPoint()
            {
                ValueInt = 5,
                ValueStr = "aa"
            };



            await resultHandler.SendResults(new SendResultsRequest()
            {
                Results = data.ToByteString()
            });

        }
    }
}
