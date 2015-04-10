using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Apis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Apprenda.Services.Logging;
using System.Diagnostics;

namespace Apprenda.SaaSGrid.Addons.Google.Compute
{
    internal class InstanceOperations
    {
        private static readonly ILogger Log = LogManager.Instance().GetLogger(typeof(GoogleCloudAddon));

        private string ProjectId { get; set; }
        private string ServiceAccountEmail { get; set; }
        private string InstanceName { get; set; }
        private string CertificateFile { get; set; }
        private string Zone { get; set; }
        private string MachineType { get; set; }
        private string SourceImage { get; set; }
        private string DiskType { get; set; }
        private string SourceImageProject { get; set; }

        internal InstanceOperations(AddonManifest manifest, GoogleCloudDeveloperOptions developerOptions)
        {
            try
            {
                var manifestprops = manifest.GetProperties().ToDictionary(x => x.Key, x => x.Value);
                ProjectId = manifestprops["ProjectID"];
                ServiceAccountEmail = manifestprops["Email"];
                CertificateFile = manifestprops["CertFile"];

                InstanceName = developerOptions.InstanceName;
                SourceImage = developerOptions.SourceImage;
                DiskType = developerOptions.DiskType;
                Zone = developerOptions.Zone;
                MachineType = developerOptions.MachineType;
                SourceImageProject = developerOptions.SourceImageProject;
            }

            catch (Exception e)
            {
                throw new ArgumentException("Argument syntax is incorrect - " + e.Message);
            }
        }

        private async Task AddInstanceTask()
        {
     
            //credentials for certificate-based service accounts
            var certificate = new X509Certificate2(CertificateFile, "notasecret", X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new ServiceAccountCredential(
            new ServiceAccountCredential.Initializer(ServiceAccountEmail)
            {
                Scopes = new[] { ComputeService.Scope.Compute, ComputeService.Scope.CloudPlatform, ComputeService.Scope.DevstorageFullControl, "https://www.googleapis.com/auth/logging.write" }
            }.FromCertificate(certificate));

            var service = new ComputeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Apprenda Addon",
            });
            var newInstance = new Instance
            {
                Disks = new List<AttachedDisk>
                {
                    new AttachedDisk
                    {
                        Type = "PERSISTENT",
                        Boot = true,
                        Mode = "READ_WRITE",
                        DeviceName = "root-disk",
                        AutoDelete = true,
                        InitializeParams = new AttachedDiskInitializeParams 
                        {
                            SourceImage = "projects/" + SourceImageProject + "-cloud/global/images/" + SourceImage,
                            DiskType = "zones/" + Zone + "/diskTypes/" + DiskType
                        }
                    }
                },
                NetworkInterfaces = new List<NetworkInterface>
                { 
                    new NetworkInterface
                    {
                        Network = "global/networks/default",
                        AccessConfigs = new List<AccessConfig>
                        {
                            new AccessConfig 
                            { 
                                Name = "External NAT", 
                                Type = "ONE_TO_ONE_NAT" 
                            }
                        }
                    }
                },
                Zone = "zones/" + Zone,
                CanIpForward = false,
                Scheduling = new Scheduling
                {
                    OnHostMaintenance = "MIGRATE",
                    AutomaticRestart = true
                },
                Name = InstanceName,
                MachineType = "zones/" + Zone + "/machineTypes/" + MachineType,
                ServiceAccounts = new List<ServiceAccount> 
                {
                    new ServiceAccount
                    {
                        Email = "default",
                        Scopes = new[] {  "https://www.googleapis.com/auth/devstorage.read_only", "https://www.googleapis.com/auth/logging.write"}
                    }
                }
            };
            try
            {
                var newInstanceQuery = await new InstancesResource.InsertRequest(service, newInstance, ProjectId, Zone).ExecuteAsync();
            }
            catch (AggregateException e)
            {
                throw new AggregateException(e);
            }
        }

        private async Task RemoveInstanceTask()
        {
            //credentials for certificate-based service accounts
            var certificate = new X509Certificate2(CertificateFile, "notasecret", X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new ServiceAccountCredential(
            new ServiceAccountCredential.Initializer(ServiceAccountEmail)
            {
                Scopes = new[] { ComputeService.Scope.Compute, ComputeService.Scope.CloudPlatform, ComputeService.Scope.DevstorageFullControl, "https://www.googleapis.com/auth/logging.write" }
            }.FromCertificate(certificate));

            var service = new ComputeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Apprenda Addon",
            });
            var startTime = DateTime.UtcNow;
         //   while(DateTime.UtcNow - startTime < TimeSpan.FromSeconds(60))
            while (true)
            {
                var instance = new InstancesResource.GetRequest(service, ProjectId, Zone, InstanceName).Execute();
                if (instance.Status == "RUNNING")
                    break;
                if(DateTime.UtcNow - startTime < TimeSpan.FromMinutes(2))
                {
                    throw new Exception("Remove instance timed out\n");
                }
            }

            try
            {
                var removeInstanceQuery = await new InstancesResource.DeleteRequest(service, ProjectId, Zone, InstanceName).ExecuteAsync();
            }
            catch(AggregateException e)
            {
                throw new AggregateException(e);
            }
        }

        internal void AddInstance()
        {
            try
            {
                AddInstanceTask().Wait();
            }
            catch (AggregateException e)
            {
                throw new AggregateException(e);
            }
        }

        internal void RemoveInstance()
        {
            try
            {
                RemoveInstanceTask().Wait();
            }
            catch (AggregateException e)
            {
                throw new AggregateException(e);
            }
        }
    }
}
