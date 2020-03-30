using System;
using System.Text;
using System.Threading;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System.Security;
using System.Management;

namespace WqlTest
{
    public static class Test1
    {
        public static void Run()
        {
            string computer = "BRN-DVB-JKRIVA";
            string domain = "swdev.local";
            string username = "jan.krivanek";

            string pid = "21344";
            var res = GetProcessInfoByPID(pid);

            string plaintextpassword;

            Console.WriteLine("Enter password:");
            plaintextpassword = Console.ReadLine();

            SecureString securepassword = new SecureString();
            foreach (char c in plaintextpassword)
            {
                securepassword.AppendChar(c);
            }

            // create Credentials
            CimCredential Credentials = new CimCredential(PasswordAuthenticationMechanism.Default,
                                                          domain,
                                                          username,
                                                          securepassword);

            // create SessionOptions using Credentials
            WSManSessionOptions SessionOptions = new WSManSessionOptions();
            SessionOptions.AddDestinationCredentials(Credentials);

            // create Session using computer, SessionOptions
            CimSession Session = CimSession.Create(computer, SessionOptions);

            var allVolumes = Session.QueryInstances(@"root\cimv2", "WQL", "SELECT * FROM Win32_Volume");
            var allPDisks = Session.QueryInstances(@"root\cimv2", "WQL", "SELECT * FROM Win32_DiskDrive");

            // Loop through all volumes
            foreach (CimInstance oneVolume in allVolumes)
            {
                // Show volume information

                if (oneVolume.CimInstanceProperties["DriveLetter"].ToString()[0] > ' ')
                {
                    Console.WriteLine("Volume ‘{0}’ has {1} bytes total, {2} bytes available",
                                      oneVolume.CimInstanceProperties["DriveLetter"],
                                      oneVolume.CimInstanceProperties["Size"],
                                      oneVolume.CimInstanceProperties["SizeRemaining"]);
                }

            }

            // Loop through all physical disks
            foreach (CimInstance onePDisk in allPDisks)
            {
                // Show physical disk information
                Console.WriteLine("Disk {0} is model {1}, serial number {2}",
                                  onePDisk.CimInstanceProperties["DeviceId"],
                                  onePDisk.CimInstanceProperties["Model"].ToString().TrimEnd(),
                                  onePDisk.CimInstanceProperties["SerialNumber"]);
            }
        }



        public static object GetProcessInfoByPID(string PID) //, out string OwnerSID)
        {
            string User;
            string Domain;
            string OwnerSID = string.Empty;
            ConnectionOptions connection = new ConnectionOptions();
            ConnectionOptions options = new ConnectionOptions();
            string targetIpAddress = "localhost";   // Pass a valid IP address (sample only)
            connection.Authentication = System.Management.AuthenticationLevel.Packet;
            ManagementScope scope = new ManagementScope(ManagementPath.DefaultPath, options);
            scope.Connect();
            //ManagementPath p = new ManagementPath("Win32_Product");
            //ManagementClass classInstance = new ManagementClass(scope, p, null);
            ObjectQuery query = new ObjectQuery("Select * from Win32_Process");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject oReturn in searcher.Get())
            {
                //Name of process
                //arg to send with method invoke to return user and domain - below is link to SDK doc on it
                string[] o = new String[2];
                //Invoke the method and populate the o var with the user name and domain
                oReturn.InvokeMethod("GetOwner", (object[])o);
                var pid = oReturn["ProcessID"];
                var processname = (string)oReturn["Name"];
                //dr[2] = oReturn["Description"];
                User = o[0];
                if (User == null)
                    User = String.Empty;
                Domain = o[1];
                if (Domain == null)
                    Domain = String.Empty;
                string[] sid = new String[1];
                oReturn.InvokeMethod("GetOwnerSid", (object[])sid);
                OwnerSID = sid[0];
                return OwnerSID;
            }
            return OwnerSID;
        }
    }
}
