using System;
using Apprenda.Services.Logging;
using Google.Apis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Util.Store;

namespace Apprenda.SaaSGrid.Addons.Google.Compute
{
    public class GoogleCloudAddon : AddonBase
    {
        private static readonly ILogger Log = LogManager.Instance().GetLogger(typeof(GoogleCloudAddon));

        public override OperationResult Deprovision(AddonDeprovisionRequest request)
        {
            throw new NotImplementedException();
        }

        public override ProvisionAddOnResult Provision(AddonProvisionRequest request)
        {
          /*  var provisionResult = new ProvisionAddOnResult("") { IsSuccess = false };
            var manifest = request.Manifest;
            var developerParameters = request.DeveloperParameters;
            var developerOptions = GoogleStorageDeveloperOptions.Parse(developerParameters);

            try
            {
                //add a bucket
                var op = new InstanceOperations(manifest, developerOptions);
                op.AddBucket();
                provisionResult.IsSuccess = true;
                provisionResult.ConnectionData = "BucketName=" + developerOptions.BucketName;
                provisionResult.EndUserMessage = "Successfully added bucket " + developerOptions.BucketName + "\n";
            }
            catch (Exception e)
            {
                provisionResult.EndUserMessage = e.Message;
                provisionResult.IsSuccess = false;
            }
            return provisionResult;*/
            throw new NotImplementedException();
        }

        public override OperationResult Test(AddonTestRequest request)
        {
            var testProgress = "";
            var testResult = new OperationResult { IsSuccess = false };
         
            var manifest = request.Manifest;
            
            var developerParameters = request.DeveloperParameters;
            var developerOptions = GoogleCloudDeveloperOptions.Parse(developerParameters, manifest);
            testProgress += "Attempting to add an instance...";
            try
            {
                var op = new InstanceOperations(manifest, developerOptions);
                op.AddInstance();
                testProgress += "Sucessfully added instance.\n";
                testResult.IsSuccess = true;
            }
            catch(Exception e)
            {
                Log.Error("Error occurred during test of Google Cloud Addon", e);
                testProgress += "EXCEPTION: " + e + "\n";
                testProgress += "Failed to add instance \n";
            }
            testResult.EndUserMessage = testProgress;
            return testResult;
        }
    }
}
