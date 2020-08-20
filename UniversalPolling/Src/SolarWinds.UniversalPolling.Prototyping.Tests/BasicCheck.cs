using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Google.Protobuf;
using Newtonsoft.Json;
using Serilog.Events;
using SolarWinds.Credentials.Snmp;
using SolarWinds.Credentials.Snmp.Contexts;
using SolarWinds.UniversalMessageFormat;
using SolarWinds.UniversalPolling.Abstractions;
using SolarWinds.UniversalPolling.Abstractions.Communication;
using SolarWinds.UniversalPolling.Abstractions.Security;
using SolarWinds.UniversalPolling.Builder;
using SolarWinds.UniversalPolling.Client;
using SolarWinds.UniversalPolling.Components.CallbackResultsHandler.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.CallbackResultsHandler.Callback;
using SolarWinds.UniversalPolling.Components.CredentialSynchronizer.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.CredentialSynchronizer.Credentials.Data;
using SolarWinds.UniversalPolling.Components.CsharpLogicExecutor.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.Echo.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.IpResolver.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.JobDispatcher.BuilderInstaller;
using SolarWinds.UniversalPolling.Components.JobDispatcher.Protobuf;
using SolarWinds.UniversalPolling.Components.JobDispatcher.TestHelpers;
using SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf;
using SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf.ClientApi;
using SolarWinds.UniversalPolling.Components.Snmp.BuilderInstaller;
using SolarWinds.UniversalPolling.JobDispatcher.Client.InternalApi;
using SolarWinds.UniversalPolling.MessageBus.InProcMessageBus;
using SolarWinds.UniversalPolling.Prototyping.HwhMonitoringLogic;
using SolarWinds.UniversalPolling.Prototyping.Tests.Utils;
using SolarWinds.UniversalPolling.PublicInterfaces;
using SolarWinds.UniversalPolling.ResultsHandler.Client.ClientApi;
using SolarWinds.UniversalPolling.ResultsHandler.Client.InternalApi;
using Xunit;
using Xunit.Abstractions;
using SendResultsStatus = SolarWinds.UniversalPolling.Components.ResultsHandler.Protobuf.SendResultsStatus;

namespace SolarWinds.UniversalPolling.Prototyping.Tests
{

    public class BasicCheck
    {
        private readonly ITestOutputHelper testOutputHelper;

        public BasicCheck(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }


        [Fact]
        public async Task BuilderTest_MonitoringLogicToApiToResultHandler__Base()
        {
            var builder = new UniversalPollingBuilder();
            builder.AddLogger(LoggerHelper.CreateLoggerFactory(testOutputHelper));
            builder.MessageBus().UseInProcMessageBus();
            builder.Modules().AddCallbackResultsHandler();

            using IUniversalPolling universalPolling = builder.Start();

            ResultsHandlerClientInternalApi api = universalPolling.GetResultsHandlerClientInternalApi();

            string testToken = universalPolling.Security.GenerateJWToken(new List<UniversalPollingJWTClaim>(), 1);

            SendResultsResponseMessage result = await api.SendResults(new SendResultsRequestMessage
            {
                PayloadData = { Results = ByteString.CopyFrom(0, 0, 0) },
                UmfMessage = { [UniversalPollingMessageHeaders.UmfHeaderSwUniversalPollingToken] = testToken }
            });


            result.PayloadData.SendStatus.Should().Be(SendResultsStatus.Ok);
        }


        [Fact]
        public async Task BuilderTest_MonitoringLogicToApiToResultHandler()
        {
            byte[] capturedResults = null;
            List<ResultHeader> capturedHeaders = null;

            void Callback(byte[] bytes, List<ResultHeader> headers)
            {
                capturedResults = bytes;
                capturedHeaders = headers;
            }

            var builder = new UniversalPollingBuilder();
            builder.AddLogger(LoggerHelper.CreateLoggerFactory(testOutputHelper));
            builder.MessageBus().UseInProcMessageBus();
            builder.Modules().AddCallbackResultsHandler(Callback);

            var monitoringPlugin = new MySampleHwhMonitoringLogic();
            builder.Modules().AddJobDispatcher()
                   .AddCsharpLogicExecutor(config =>
                   {
                       config.PluginsForRegistration = new List<MonitoringPluginRegistration>
                       {
                               new MonitoringPluginRegistration
                               {
                                   PluginName = "myHwh_plugin",
                                   MonitoringPlugin = monitoringPlugin,
                                   CategoryName = "myHwh_category"
                               }
                       };
                   });

            using IUniversalPolling universalPolling = builder.Start();

            await Task.Delay(50);

            JobDispatcherClientInternalApi api = universalPolling.GetJobDispatcherClientInternalApi();

            var request = new RunMonitoringPluginTaskRequestMessage
            {
                PayloadData =
                {
                    PluginName = "myHwh_plugin", StartParameters = new StartParameters
                    {
                        TtlInMinutes = 1,
                    }
                }
            };
            request.PayloadData.StartParameters.DeviceIds.Add("1");
            request.PayloadData.StartParameters.AdditionalRoles.Add(1);

            await api.RunPluginTask(request);

            await Task.Delay(200);

            monitoringPlugin.Executed.Should().BeTrue();
        }



        [Fact]
        public async Task BuilderTest_ViptelaMemoryMonitoringLogicToApiToResultHandler()
        {
            byte[] capturedResults = null;
            List<ResultHeader> capturedHeaders = null;

            void Callback(byte[] bytes, List<ResultHeader> headers)
            {
                capturedResults = bytes;
                capturedHeaders = headers;
            }

            var builder = new UniversalPollingBuilder();
            builder.AddLogger(LoggerHelper.CreateLoggerFactory(testOutputHelper));
            builder.MessageBus().UseInProcMessageBus();
            builder.Modules().AddCallbackResultsHandler(Callback);

            var monitoringPlugin = new ViptelaCpuMonitoringLogic();
            builder.Modules().AddEcho();
            builder.Modules().AddJobDispatcher()
                   .AddCsharpLogicExecutor(config =>
                   {
                       config.PluginsForRegistration = new List<MonitoringPluginRegistration>
                       {
                               new MonitoringPluginRegistration
                               {
                                   PluginName = "VipteraMemory_plugin",
                                   MonitoringPlugin = monitoringPlugin,
                                   CategoryName = "VipteraMemory_plugin"
                               }
                       };
                   });

            var snmpContext = new SnmpV2ConnectionContext
            {
                ConnectionProfile = new SnmpConnectionProfile
                {
                    AgentPort = 161,
                    InterQueryDelayInMillisecond = 10
                },
                Credentials = new SnmpCredentialsV2
                {
                    Community = "public"
                }
            };
            builder.Modules().AddIpResolver(config =>
            {
                config.Hostnames = new List<HostnameEntry>
                {
                    new HostnameEntry
                    {
                        DeviceId = "1",
                        Hostname = "10.199.2.109"
                    }
                };
            }).AddCredentialSynchronizer(config =>
            {
                config.Credentials = new List<CredentialsData>
                {
                    new CredentialsData
                    {
                        Hostname = "10.199.2.109",
                        Id = "1",
                        Type = "snmp",
                        Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snmpContext))
                    }
                };
            }).AddSnmp();

            using IUniversalPolling universalPolling = builder.Start();

            await Task.Delay(50);

            JobDispatcherClientInternalApi api = universalPolling.GetJobDispatcherClientInternalApi();

            var request = new RunMonitoringPluginTaskRequestMessage
            {
                PayloadData =
                {
                    PluginName = "VipteraMemory_plugin", StartParameters = new StartParameters
                    {
                        TtlInMinutes = 1,
                    }
                }
            };
            request.PayloadData.StartParameters.DeviceIds.Add("1");
            request.PayloadData.StartParameters.AdditionalRoles.Add(1);

            await api.RunPluginTask(request);

            await Task.Delay(20000);

            monitoringPlugin.Executed.Should().BeTrue();
        }
    }
}
