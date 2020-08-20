using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SolarWinds.UniversalPolling.Client;
using SolarWinds.UniversalPolling.Components.Echo.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Components.Snmp.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Echo.Client.ClientApi;
using SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic.Protobuf;
using SolarWinds.UniversalPolling.ResultsHandler.Client.ClientApi;
using SolarWinds.UniversalPolling.Snmp.Client.ClientApi;

namespace SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic
{
    public class ViptelaCpuMonitoringLogic : IMonitoringPlugin
    {
        private const string _cpu_oid = "1.3.6.1.4.1.41916.11.1.14.0";

        public async Task Execute(IMonitoringFramework monitoringFramework, CancellationToken cancellationToken)
        {
            ResultsHandlerClient resultHandler = monitoringFramework.GetResultsHandlerClient();

            MultiCoreCpuLoadDataPoint result;
            try
            {
                SnmpClient snmp = monitoringFramework.GetSnmpClient();
                result = await PollDataInternal(snmp).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //todo: log
                result = new MultiCoreCpuLoadDataPoint()
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

        private async Task<MultiCoreCpuLoadDataPoint> PollDataInternal(SnmpClient snmp)
        {
            var response = await snmp.GetOid(new GetOidRequest()
                {
                    Oid = _cpu_oid
                })
                .ConfigureAwait(false);

            MultiCoreCpuLoadDataPoint result = new MultiCoreCpuLoadDataPoint();

            result.IsError = response.ResultStatus != ResultStatus.OK;
            //protobuf would throw on null assignment
            if (response.ErrorMessage != null) result.ErrorMessage = response.ErrorMessage;

            if (response.ResultStatus == ResultStatus.OK)
            {
                string pollValue = response.Response.Result.Value;
                int cpuValue = (int)double.Parse(pollValue);
                result.CpuLoadPerIndex[0] = cpuValue;
            }

            return result;
        }
    }
}
