﻿using System;
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
        public bool Executed { get; set; }
        private const string _cpu_oid = "1.3.6.1.4.1.41916.11.1.14.0";

        public async Task Execute(IMonitoringFramework monitoringFramework, CancellationToken cancellationToken)
        {
            Executed = true;

            ResultsHandlerClient resultHandler = monitoringFramework.GetResultsHandlerClient();

            var echo = monitoringFramework.GetEchoClient();
            var rsp = await echo.SendEcho(new EchoRequest() {Text = "aaadd"});
            

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
                    Results = ByteString.Empty //result.ToByteString()
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

            if (response.ResultStatus == ResultStatus.OK)
            {
                string pollValue = response.Response.Result.Value;
                int cpuValue = (int)double.Parse(pollValue);
                result.CpuLoadPerIndex[0] = cpuValue;
            }
            else
            {
                result.ErrorMessage = response.ErrorMessage;
            }

            return result;
        }
    }
}