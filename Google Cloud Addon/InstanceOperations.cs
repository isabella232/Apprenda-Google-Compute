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


namespace Apprenda.SaaSGrid.Addons.Google.Compute
{
    internal class InstanceOperations
    {
        private string ProjectId { get; set; }
        private string ServiceAccountEmail { get; set; }
        private string InstanceName { get; set; }
        private string CertificateFile { get; set; }
        private string Zone { get; set; }
        private string MachineType { get; set; }
        private string SourceImage { get; set; }
        private string DiskType { get; set; }

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
                Name = InstanceName,
                MachineType = "zones/" + Zone + "/machineTypes/" + MachineType,
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
                            SourceImage = "projects/debian-cloud/global/images/" + SourceImage,
                            DiskType = "zones/" + Zone + "/diskTypes/" + DiskType
                        }
                    }
                },
                ServiceAccounts = new List<ServiceAccount> 
                {
                    new ServiceAccount
                    {
                        Email = "default",
                        Scopes = new[] {  "https://www.googleapis.com/auth/devstorage.read_only", "https://www.googleapis.com/auth/logging.write"}
                    }
                },
                CanIpForward = false,
                Scheduling = new Scheduling
                {
                    OnHostMaintenance = "MIGRATE",
                    AutomaticRestart = true
                }
            };

            try
            {
                var newInstanceQuery = await new InstancesResource.InsertRequest(service, newInstance, ProjectId, Zone).ExecuteAsync();        
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
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

            try
            {
                var removeInstanceQuery = await new InstancesResource.DeleteRequest(service, ProjectId, Zone, InstanceName).ExecuteAsync();
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal void AddInstance()
        {
            try
            {
                AddInstanceTask().Wait();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal void RemoveInstance()
        {
            try
            {
                RemoveInstanceTask().Wait();
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
