using System;
using Apprenda.Services.Logging;
using System.Threading;

namespace Apprenda.SaaSGrid.Addons.Google.Compute
{
    public class GoogleCloudAddon : AddonBase
    {
        private static readonly ILogger Log = LogManager.Instance().GetLogger(typeof(GoogleCloudAddon));

        public override OperationResult Deprovision(AddonDeprovisionRequest request)
        {
            string connectionData = request.ConnectionData;
            var deprovisionResult = new ProvisionAddOnResult(connectionData);
            AddonManifest manifest = request.Manifest;
            var developerParameters = request.DeveloperParameters;
            var developerOptions = GoogleCloudDeveloperOptions.Parse(developerParameters);
            try
            {
                var conInfo = ConnectionInfo.Parse(connectionData);
                developerOptions.InstanceName = conInfo.InstanceName;
                developerOptions.Zone = conInfo.Zone;
                var op = new InstanceOperations(manifest, developerOptions);
                op.RemoveInstance();
                deprovisionResult.IsSuccess = true;
                deprovisionResult.EndUserMessage = "Successfully deleted instance: " + conInfo.InstanceName;
            }
            
            catch (Exception e)
            {
                deprovisionResult.EndUserMessage = e.Message;
                deprovisionResult.IsSuccess = false;
            }
            return deprovisionResult;
        }

        public override ProvisionAddOnResult Provision(AddonProvisionRequest request)
        {
            var provisionResult = new ProvisionAddOnResult("") { IsSuccess = false };
            var manifest = request.Manifest;
            var developerParameters = request.DeveloperParameters;
            var developerOptions = GoogleCloudDeveloperOptions.Parse(developerParameters);
            try
            {
                //add instance
                var op = new InstanceOperations(manifest, developerOptions);
                op.AddInstance();
                provisionResult.IsSuccess = true;
                provisionResult.ConnectionData = "InstanceName=" + developerOptions.InstanceName + "&Zone=" + developerOptions.Zone;
                provisionResult.EndUserMessage = "Successfully added instance " + developerOptions.InstanceName + "\n";
            }
            catch (Exception e)
            {
                provisionResult.EndUserMessage = e.Message;
                provisionResult.IsSuccess = false;
            }
            return provisionResult;
        }

        public override OperationResult Test(AddonTestRequest request)
        {
            var testProgress = "";
            var testResult = new OperationResult { IsSuccess = false };
         
            var manifest = request.Manifest;
            
            var developerParameters = request.DeveloperParameters;
            var developerOptions = GoogleCloudDeveloperOptions.Parse(developerParameters);
            testProgress += "Attempting to add an instance...";
            try
            {
                //add instance
                var op = new InstanceOperations(manifest, developerOptions);
                op.AddInstance();
                testProgress += "Successfully added instance.\n";
                //wait for instance to finishing adding
                Thread.Sleep(120000);

                try
                {
                    //remove instance
                    testProgress += "Attempting to remove an instance...";
                    op.RemoveInstance();
                    testProgress += "Successfully removed instance.\n";
                    testResult.IsSuccess = true;
                }
                catch(AggregateException e)
                {
                    foreach (var err in e.InnerExceptions)
                    {
                        Log.Error("Error occurred during test of Google Cloud Addon", err);
                        testProgress += "EXCEPTION: " + err + "\n";
                    }
                    testProgress += "Failed to remove instance \n";
                }
            }
            catch (AggregateException e)
            {
                foreach (var err in e.InnerExceptions)
                {
                    Log.Error("Error occurred during test of Google Cloud Addon", err);
                    testProgress += "EXCEPTION: " + err + "\n";
                }
                testProgress += "Failed to add instance \n";
            }
            testResult.EndUserMessage = testProgress;
            return testResult;
        }
    }
}
