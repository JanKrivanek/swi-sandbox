using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SolarWinds.UniversalPolling.Client;
using SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Components.Snmp.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic.Protobuf;
using SolarWinds.UniversalPolling.ResultsHandler.Client.ClientApi;
using SolarWinds.UniversalPolling.Snmp.Client.ClientApi;

namespace SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic
{
    public class ViptelaMemoryMonitoringLogic : IMonitoringPlugin
    {
        private const string _memtotal_oid = "1.3.6.1.4.1.41916.11.1.17.0";
        private const string _memused_oid = "1.3.6.1.4.1.41916.11.1.18.0";

        public async Task Execute(IMonitoringFramework monitoringFramework, CancellationToken cancellationToken)
        {
            ResultsHandlerClient resultHandler = monitoringFramework.GetResultsHandlerClient();

            BasicMemoryDataPoint result;
            try
            {
                SnmpClient snmp = monitoringFramework.GetSnmpClient();
                result = await PollDataInternal(snmp).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //todo: log
                result = new BasicMemoryDataPoint()
                {
                    IsError = true,
                    ErrorMessage = e.Message,
                };
            }

            await resultHandler.SendResults(new SendResultsRequest()
            {
                Results = result.ToByteString()
            })
                .ConfigureAwait(false);
        }

        private async Task<BasicMemoryDataPoint> PollDataInternal(SnmpClient snmp)
        {
            var response = await snmp.GetOids(new GetOidsRequest()
                {
                    Oids = {_memtotal_oid, _memused_oid}
                })
                .ConfigureAwait(false);

            BasicMemoryDataPoint result = new BasicMemoryDataPoint();

            result.IsError = response.ResultStatus != ResultStatus.OK;
            //protobuf would throw on null assignment
            if(response.ErrorMessage != null) result.ErrorMessage = response.ErrorMessage;

            if (response.ResultStatus == ResultStatus.OK)
            {
                result.TotalMemory = PolledValueToKb(response.Response.Results[0]);
                result.UsedMemory = PolledValueToKb(response.Response.Results[1]);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double PolledValueToKb(COid coid)
        {
            return double.Parse(coid.Value) * 1024.0;
        }
    }
}
