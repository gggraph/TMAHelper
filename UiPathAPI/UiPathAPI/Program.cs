using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace UiPathAPI
{
    
    class Program
    {
        static void Main(string[] args)
        {
            string organization = "coexya";
            string tenant = "COEXYA";
            string appID = "6bbc508b-de86-4766-9e61-23a429550d0f";
            string appSecret = "~*#V~TS_ylM(1jXu";
            UiPathConnector myUiPathConnector = new UiPathConnector(organization, tenant, appID, appSecret);
            Console.WriteLine(myUiPathConnector.GetAssetValue("Authentication_ExternalApp", "AppID"));
            List<JObject> logs = myUiPathConnector.GetLogsDetails("Shared");
            Console.WriteLine("logs content : ");
            foreach (JObject log in logs) 
            {
                string msg = log["message"].ToString();

                if (msg.Length > 50)
                    msg = msg.Substring(0, 50);

                Console.WriteLine("Time : " + DateTime.Parse(log["timeStamp"].ToString()).ToString() + " Message : " + msg); 
            }
            while (true) { }
        } 
    }
    public class UiPathConnector 
    {
        private Dictionary<string, string> folders = new Dictionary<string, string>();
        private string organizationID;
        private string tenantName;
        private string bearer;
        private string appID;
        private string appSecret;

        // Auth from here : https://docs.uipath.com/orchestrator/reference/using-oauth-for-external-apps
        public UiPathConnector(string org, string tenant, string appID, string appSecret) 
        {
            organizationID = org;
            tenantName = tenant;
            this.appID = appID;
            this.appSecret = appSecret;

            bearer = GetBearer();
            JToken JFolders = GetFolders();
            foreach( JToken folder in JFolders) 
                folders.Add(folder["DisplayName"].ToString(), folder["Id"].ToString());
            
        }

        private string GetBearer() 
        {
            Dictionary<string, string> urlParts = new Dictionary<string, string>
            {
              { "grant_type", "client_credentials" },
              { "client_id", appID },
              { "client_secret", appSecret },
              { "scope", "OR.Default OR.Folders OR.Assets OR.Monitoring" }
            };
            Task<string> request = NetInterface.Send(
                HttpMethod.Post,
                "https://cloud.uipath.com/identity_/connect/token",
                null,
                urlParts,
                null);

            request.Wait();
            string response = request.Result;
            JObject jObject = JObject.Parse(response);
            return jObject["access_token"].ToString();
        }

        public void SetTenantTarget(string organization, string tenant) 
        {
            organizationID = organization;
            tenantName = tenant;
            folders = new Dictionary<string, string>();
            JToken JFolders = GetFolders();
            foreach (JToken folder in JFolders)
                folders.Add(folder["DisplayName"].ToString(), folder["Id"].ToString());
        }

        // most call from here : https://cloud.uipath.com/coexya/COEXYA//swagger/index.html
        public JToken GetFolders()
        {
            Task<string> request = NetInterface.Send(
                HttpMethod.Get,
                "https://cloud.uipath.com/" + organizationID + "/" + tenantName + "/odata/Folders",
                null, 
                null,
                bearer) ;

            request.Wait();
            string response = request.Result;
           
            JObject jObject = JObject.Parse(response);
            return jObject["value"];
        }

        public string GetFolderID(string folderName) 
        {
            JToken folders = GetFolders();
            foreach ( JToken folder in folders) 
            {
                if (folder["DisplayName"].ToString() == folderName)
                    return folder["Id"].ToString();
            }
            return null;
        }

        public JToken GetAssets(string folderID)
        {
            Task<string> request = NetInterface.Send( 
                HttpMethod.Get,
                "https://cloud.uipath.com/" + organizationID + "/" + tenantName + "/odata/Assets",
                new Dictionary<string, string>() { { "X-UIPATH-OrganizationUnitId", folderID } },
                null,
                bearer);
            request.Wait();
            string response = request.Result;
            JObject jObject = JObject.Parse(response);
            return jObject["value"];
        }
        public JToken GetAsset(string folderName, string AssetName)
        {
            string folderID = string.Empty;
            if (folders.ContainsKey(folderName))
                folderID = folders[folderName];
            JToken assets = GetAssets(folderID);
            foreach ( JToken asset in assets) 
            {
                if (asset["Name"].ToString() == AssetName)
                    return asset;
            }
            return null;
            
        }
        public string GetAssetValue(string folderName, string AssetName)
        {
            return GetAsset(folderName,AssetName)["Value"].ToString();
        }
        public JToken GetLogs(string folderName)
        {
            string folderID = string.Empty;
            if (folders.ContainsKey(folderName))
                folderID = folders[folderName];
            Task<string> request = NetInterface.Send(
                HttpMethod.Get,
                "https://cloud.uipath.com/" + organizationID + "/" + tenantName + "/odata/RobotLogs",
                new Dictionary<string, string>() { { "X-UIPATH-OrganizationUnitId", folderID } },
                new Dictionary<string, string>() { },
                bearer);
            request.Wait();
            string response = request.Result;
            JObject jObject = JObject.Parse(response);
            return jObject["value"];
        }
        public List<JObject> GetLogsDetails(string folderName) 
        {
            JToken logs = GetLogs(folderName);
            List<JObject> logDetails = new List<JObject>();
            foreach ( JToken log in logs) 
            {
                JObject jObject = JObject.Parse(log["RawMessage"].ToString());
                logDetails.Add(jObject);
            } 
                
            return logDetails;
        }

    }
    public static class NetInterface
    {
        public static readonly HttpClient client = new HttpClient();
        public async static Task<string> Send
            (
            HttpMethod netMethod,
            string endPoint,
            Dictionary<string, string> headerContents,
            Dictionary<string, string> urlContents,
            string bearer
            ) 
        {
            if (bearer != null)
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);

            var content = new FormUrlEncodedContent(urlContents == null ? new Dictionary<string,string>() : urlContents);

            if (headerContents != null)
            {
                foreach (KeyValuePair<string, string> kvp in headerContents)
                    content.Headers.Add(kvp.Key, kvp.Value);
            }
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri(endPoint);
            request.Method = netMethod;
            request.Content = content;
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }
     
    } 
}
