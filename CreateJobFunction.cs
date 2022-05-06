using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FHSCAzureFunction.Models;
using System.Collections.Generic;
using System.Linq;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Hosting;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using CsvHelper;
using CsvHelper.Configuration;
using System.Reflection;
using System.Globalization;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;
using FHSCAzureFunction.Models.Configs;
using System.Diagnostics;
using System.Net.Http;

namespace FHSCAzureFunction
{
    public class CreateJobFunction
    {
        private readonly Stopwatch stopWatch = new Stopwatch();
        private readonly FlocHierarchyDBContext dbContext;
        private readonly IWebHostEnvironment webEnvironment;
        private readonly BlobStorageService blobStorageService;

        SDxConfig config;
        string Token;

        public CreateJobFunction(FlocHierarchyDBContext context, IWebHostEnvironment env, SDxConfig sDxConfig)
        {
            dbContext = context;
            webEnvironment = env;
            config = sDxConfig;
            blobStorageService = new BlobStorageService();
        }

        [FunctionName("CreateJobFunction")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic tempdata = JsonConvert.DeserializeObject(requestBody);
                RequestData data = JsonConvert.DeserializeObject<RequestData>(tempdata);
                var base64EncodedBytes = System.Convert.FromBase64String(data.EncodedAccessToken);
                Token = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                //Token = data.EncodedAccessToken;

                stopWatch.Reset();
                stopWatch.Start();
                //ViewBag.message = "Job added successfully";
                await ValidateGsapDataAsync(data, log);
            }
            catch (Exception)
            {
            }
        }


        //Method to validate if the given and actual floc levels are same
        bool Valid(GsaporiginalData floc, string floclevel)
        {
            if (floc.SortField == floclevel)
                return true;
            else
                return false;
        }

        //Method to retrieve Floc 1(Terminal) details from SDx 
        async Task<List<Floc1Data>> GetFloc1DetailsFromSDx(string Pbstype)
        {
            try
            {
                List<Floc1Data> Floc1 = new List<Floc1Data>();
                //Defining the URLs

                string OdataQuery = config.ServerBaseUri + Pbstype + $"?$count=true";
                string OdataQuery1 = config.ServerBaseUri + Pbstype + $"?$select=Name,Terminal_Description,Country_Code&$expand=SDAUnit($select=Cluster)&$top=";

                //AMERICAS Region
                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; },
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };
                var client = new RestClient(clientHandler).UseNewtonsoftJson();

                var request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of Floc1 records
                var response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                var count = response.Count;

                //Appending the count to the 2nd query to get the details including name, description, country and cluster
                string NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                Floc1.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                    Floc1.AddRange(response.Value);
                }

                //EUSA Region
                request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_EUSA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of Floc1 records
                response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                count = response.Count;

                //Appending the count to the 2nd query to get the details including name, description, country and cluster
                NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_EUSA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                Floc1.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_EUSA");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                    Floc1.AddRange(response.Value);
                }

                //MEA Region
                request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_MEA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of Floc1 records
                response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                count = response.Count;

                //Appending the count to the 2nd query to get the details including name, description, country and cluster
                NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_MEA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                Floc1.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_MEA");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<Floc1Data>>(request);
                    Floc1.AddRange(response.Value);
                }

                return Floc1;
            }
            catch (Exception e)
            {
                throw new Exception("Could not get Floc 1 data from SDx: " + e.Message);
            }
        }

        //Method th retrieve the flocs and equipment details
        async Task<List<PbsObject>> GetFlocDetailsFromSDx(string Pbstype)
        {
            try
            {
                List<PbsObject> floc = new List<PbsObject>();
                string OdataQuery = config.ServerBaseUri + Pbstype + $"?$count=true";
                string OdataQuery1 = config.ServerBaseUri + Pbstype + $"?$select=Name&$top=";

                //AMERICAS Region
                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; },
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };
                var client = new RestClient(clientHandler).UseNewtonsoftJson();

                var request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of records
                var response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                var count = response.Count;

                //Appending the count to the 2nd query to get the details including name
                string NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                floc.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_AMERICAS");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                    floc.AddRange(response.Value);
                }

                //EUSA Region
                request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_EUSA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of records
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                count = response.Count;

                //Appending the count to the 2nd query to get the details including name
                NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_EUSA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                floc.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_EUSA");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                    floc.AddRange(response.Value);
                }

                //MEA Region
                request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_MEA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);

                count = response.Count;

                //Appending the count to the 2nd query to get the details including name
                NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.AddHeader("SPFConfigUID", "PL_MEA");
                //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                floc.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    request.AddHeader("SPFConfigUID", "PL_MEA");
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                    floc.AddRange(response.Value);
                }

                return floc;
            }
            catch (Exception e)
            {
                throw new Exception("Could not get Floc data from SDx: " + e.Message);
            }
        }

        async Task<List<PbsObject>> GetTechnicalObjTypeDataFromSDx()
        {
            string OdataQuery = config.ServerBaseUri + $"Material_Types?$count=true";
            string OdataQuery1 = config.ServerBaseUri + $"Material_Types?$select=Name&$top=";
            List<PbsObject> objType = new List<PbsObject>();

            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; },
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };
                var client = new RestClient(clientHandler).UseNewtonsoftJson();

                var request = new RestRequest(OdataQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                ////request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");

                //Executing the request and obtaining the number of records
                var response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                var count = response.Count;

                string NewQuery = OdataQuery1 + count.ToString();
                request = new RestRequest(NewQuery);
                request.AddHeader("Authorization", "Bearer " + Token);
                ////request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                objType.AddRange(response.Value);

                //Obtaining the response repeatedly if there are more than 1000 records
                while (response.NextLink != null)
                {
                    request = new RestRequest(response.NextLink);
                    request.AddHeader("Authorization", "Bearer " + Token);
                    //request.AddHeader("X-Ingr-OnBehalfOf", "SHG38P.JKundu");
                    response = await client.GetAsync<OdataQueryResponse<PbsObject>>(request);
                    objType.AddRange(response.Value);
                }

                return objType;
            }
            catch (Exception e)
            {
                throw new Exception("Could not get Techincal Object Type data from SDx: " + e.Message);
            }
        }


        //Method to read data from the CSV files
        bool ReadCSV(int id, string GsapFilePath, string EquipmentFilePath)
        {
            //Initializing the paths to the CSV files
            /*string path = "UploadedFiles/" + id.ToString();
            string GsapFilePath = path + "/GSAP_ORIGINAL_DATA";
            string EquipmentFilePath = path + "/EQUIPMENT_DATA";*/

            try
            {
                //Opening the file to read GSAP data
                using MemoryStream stream = blobStorageService.DownloadFileFromBlob(GsapFilePath);
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "GSAP_ORIGINAL_DATA").ToList();

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<GsaporiginalData>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(GsaporiginalData).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(GsaporiginalData), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Fetching the rows from the CSV file
                var records = csv.GetRecords<GsaporiginalData>().ToList();
                foreach (var item in records)
                    item.JobId = id;

                //Inserting data into database
                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not read GSAP data from file: " + e.Message);
            }

            try
            {
                using MemoryStream stream = blobStorageService.DownloadFileFromBlob(EquipmentFilePath);
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "EQUIPMENT_DATA_FROM_GSAP").ToList();

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<EquipmentDataFromGsap>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(EquipmentDataFromGsap).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(EquipmentDataFromGsap), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Fetching the rows from the CSV file
                var records = csv.GetRecords<EquipmentDataFromGsap>().ToList();
                foreach (var item in records)
                    item.JobId = id;

                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not read Equipment data from file: " + e.Message);
            }

            return true;
        }

        //Method to write valid Floc 1 details to database
        void WriteFloc1Details(List<GsaporiginalData> details, List<Floc1Data> existingFlocs, int id)
        {
            //Path where the CSV is saved
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Floc1/";

            //Getting valid floc 1 data
            var floc1Details = details.Where(x => x.ActualLevel == 1 && x.HasError == false);
            List<Floc1> records = new List<Floc1>();

            foreach (var floc in floc1Details)
            {
                //Getting the country and cluster details for exising flocs
                var pbs = existingFlocs.Where(x => x.Name == floc.FunctionalLocation).FirstOrDefault();
                Floc1 fl = new Floc1
                {
                    JobId = id,
                    TerminalCode = floc.FunctionalLocation,
                    TerminalDescription = floc.DescriptionFunctionLocation
                };
                if (pbs != null)
                {
                    fl.Cluster = pbs.SDAUnit.Cluster;
                    fl.Country = pbs.Country;
                }
                records.Add(fl);

            }
            try
            {
                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "FLOC_1_DETAILS").ToList();

                path += columns[0].CsvName + "_JobID_" + id.ToString() + ".csv";

                //Opening the file to write floc1 data
                MemoryStream ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var map = new DefaultClassMap<Floc1>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(Floc1).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(Floc1), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    newMap.Data.Index = c.CsvColSequence;
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Writing to the CSV file
                csv.WriteRecords(records);
                csv.Flush();
                writer.Flush();

                //Creating the mapping between the database columns and the CSV column names

                ms.Position = 0;
                string filepath = blobStorageService.UploadFileToBlob(path, ms, "text/csv");

                writer.Close();
                ms.Dispose();
                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not write valid Floc 1 details: " + e.Message);
            }
        }

        //Method to write valid Floc 2 details to database
        void WriteFloc2Details(List<GsaporiginalData> details, int id)
        {
            //Path where the CSV is saved
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Floc2/";

            //Getting valid floc 2 details
            var floc2Details = details.Where(x => x.ActualLevel == 2 && x.HasError == false && x.SuperiorFlocHasError == false);
            List<Floc2> records = new List<Floc2>();
            foreach (var floc in floc2Details)
            {
                Floc2 fl2 = new Floc2
                {
                    JobId = id,
                    FlocLevel2Name = floc.FunctionalLocation,
                    FlocLevel2Description = floc.DescriptionFunctionLocation,
                    TerminalCode = floc.SuperiorFunctionalLocation
                };
                records.Add(fl2);
            }
            try
            {
                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "FLOC_2_DETAILS").ToList();
                path += columns[0].CsvName + "_JobID_" + id.ToString() + ".csv";

                //Opening the file to write floc2 data
                MemoryStream ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<Floc2>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(Floc2).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(Floc2), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    newMap.Data.Index = c.CsvColSequence;
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Writing to the CSV file
                csv.WriteRecords(records);
                csv.Flush();
                writer.Flush();

                ms.Position = 0;
                string filepath = blobStorageService.UploadFileToBlob(path, ms, "text/csv");

                writer.Close();
                ms.Dispose();
                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not write valid Floc 2 details: " + e.Message);
            }
        }

        //Method to write valid Floc 3 details to database
        void WriteFloc3Details(List<GsaporiginalData> details, int id)
        {
            //Path where the CSV is saved
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Floc3/";

            //Getting valid floc 3 records
            var floc3Details = details.Where(x => x.ActualLevel == 3 && x.HasError == false && x.SuperiorFlocHasError == false);
            List<Floc3> records = new List<Floc3>();
            foreach (var floc in floc3Details)
            {
                Floc3 fl3 = new Floc3
                {
                    JobId = id,
                    FlocLevel3Name = floc.FunctionalLocation,
                    FlocLevel3Description = floc.DescriptionFunctionLocation,
                    FlocLevel2Name = floc.SuperiorFunctionalLocation
                };
                records.Add(fl3);

            }
            try
            {
                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "FLOC_3_DETAILS").ToList();
                path += columns[0].CsvName + "_JobID_" + id.ToString() + ".csv";

                //Opening the file to write floc3 data
                MemoryStream ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<Floc3>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(Floc3).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(Floc3), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    newMap.Data.Index = c.CsvColSequence;
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Writing to the CSV file
                csv.WriteRecords(records);
                csv.Flush();
                writer.Flush();

                ms.Position = 0;
                string filepath = blobStorageService.UploadFileToBlob(path, ms, "text/csv");

                writer.Close();
                ms.Dispose();
                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not write valid Floc 3 details: " + e.Message);
            }
        }

        //Method to write valid Floc 4 details to database
        void WriteFloc4Details(List<GsaporiginalData> details, int id)
        {
            //Path where the CSV is saved
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Floc4/";

            //Getting valid floc 4 records
            var floc4Details = details.Where(x => x.ActualLevel == 4 && x.HasError == false && x.SuperiorFlocHasError == false);
            List<Floc4> records = new List<Floc4>();
            foreach (var floc in floc4Details)
            {
                Floc4 fl4 = new Floc4
                {
                    JobId = id,
                    SuperiorFunctionalLocation = floc.SuperiorFunctionalLocation,
                    FunctionalLocation = floc.FunctionalLocation,
                    DescriptionFunctionLocation = floc.DescriptionFunctionLocation,
                    TechnicalObjectType = floc.TechnicalObjectType,
                    MaintenancePlant = floc.MaintenancePlant,
                    PlanningPlant = floc.PlanningPlant,
                    SortField = floc.SortField,
                    FunctionalLocationCategory = floc.FunctionalLocationCategory,
                    SystemStatus = floc.SystemStatus
                };
                records.Add(fl4);
            }

            //Inserting data into database
            try
            {
                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "GSAP_ORIGINAL_DATA").ToList();
                path += "TS FLOC Load_JobID_" + id.ToString() + ".csv";

                //Opening the file to write floc1 data
                MemoryStream ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<Floc4>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(Floc4).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(Floc4), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    newMap.Data.Index = c.CsvColSequence;
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Writing to the CSV file
                csv.WriteRecords(records);
                csv.Flush();
                writer.Flush();

                ms.Position = 0;
                string filepath = blobStorageService.UploadFileToBlob(path, ms, "text/csv");

                writer.Close();
                ms.Dispose();
                dbContext.BulkInsert(records);
            }
            catch (Exception e)
            {
                throw new Exception("Could not write valid Floc 4 details: " + e.Message);
            }
        }

        //Method to Write the valid equipment details into a separate table in database
        void WriteEquipmentDetails(int id)
        {
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Equipment/";
            List<EquipmentDataFromGsap> details = null;
            List<EquipmentDetails> validEquipments = new List<EquipmentDetails>();

            //Getting the valid eqipment details
            try
            {
                details = dbContext.EQUIPMENT_DATA_FROM_GSAP.Where(x => x.FlocHasError == false && x.ObjTypeHasError == false && x.SapHasError == false && x.JobId == id).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            foreach (var record in details)
            {
                validEquipments.Add(new EquipmentDetails
                {
                    JobId = id,
                    Equipment = record.Equipment,
                    DescriptionTechnicalObject = record.DescriptionTechnicalObject,
                    EquipmentCategory = record.EquipmentCategory,
                    TechnicalObjectType = record.TechnicalObjectType,
                    TechnicalIdentificationNo = record.TechnicalIdentificationNo,
                    FunctionalLocation = record.FunctionalLocation,
                    MaintenancePlant = record.MaintenancePlant,
                    PlanningPlant = record.PlanningPlant,
                    SystemStatus = record.SystemStatus,
                    SapId = record.SapId
                });
            }
            try
            {
                //Reading the columns information from the database
                var columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "EQUIPMENT_DATA_FROM_GSAP").ToList();
                path += "Equipment_JobID_ " + id.ToString() + ".csv";

                //Opening the file to write floc1 data
                MemoryStream ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                //Creating the mapping between the database columns and the CSV column names
                var map = new DefaultClassMap<EquipmentDetails>();
                foreach (var c in columns)
                {
                    PropertyInfo prop = typeof(EquipmentDetails).GetProperty(c.DbColName);
                    var newMap = MemberMap.CreateGeneric(typeof(EquipmentDetails), prop);
                    newMap.Data.Names.Add(c.CsvColName);
                    newMap.Data.Index = c.CsvColSequence;
                    map.MemberMaps.Add(newMap);
                }
                csv.Context.RegisterClassMap(map);

                //Writing to the CSV file
                csv.WriteRecords(validEquipments);
                csv.Flush();
                writer.Flush();

                ms.Position = 0;
                string filepath = blobStorageService.UploadFileToBlob(path, ms, "text/csv");

                writer.Close();
                ms.Dispose();
                //Writing data into table in the database
                dbContext.BulkInsert(validEquipments);
            }
            catch (Exception e)
            {
                throw new Exception("Could not write valid equipment details: " + e.Message);
            }

        }

        //Method to create the Stylesheet for excel files
        private static Stylesheet CreateStylesheet()
        {
            Stylesheet styleSheet = new Stylesheet() { MCAttributes = new MarkupCompatibilityAttributes() { Ignorable = "x14ac" } };
            styleSheet.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
            styleSheet.AddNamespaceDeclaration("x14ac", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac");

            Fonts fonts = new Fonts() { Count = (UInt32Value)1U, KnownFonts = true };

            Font font1 = new Font();
            FontSize fontSize1 = new FontSize() { Val = 11D };
            Color color1 = new Color() { Theme = (UInt32Value)1U };
            FontName fontName1 = new FontName() { Val = "Calibri" };
            FontFamilyNumbering fontFamilyNumbering1 = new FontFamilyNumbering() { Val = 2 };
            FontScheme fontScheme1 = new FontScheme() { Val = FontSchemeValues.Minor };

            font1.Append(fontSize1);
            font1.Append(color1);
            font1.Append(fontName1);
            font1.Append(fontFamilyNumbering1);
            font1.Append(fontScheme1);
            fonts.Append(font1);

            Font font2 = new Font();
            FontSize fontSize2 = new FontSize() { Val = 11D };
            Color color2 = new Color() { Rgb = HexBinaryValue.FromString("FF3300") };
            FontName fontName2 = new FontName() { Val = "Calibri" };
            FontFamilyNumbering fontFamilyNumbering2 = new FontFamilyNumbering() { Val = 2 };
            FontScheme fontScheme2 = new FontScheme() { Val = FontSchemeValues.Minor };

            font2.Append(fontSize2);
            font2.Append(color2);
            font2.Append(fontName2);
            font2.Append(fontFamilyNumbering2);
            font2.Append(fontScheme2);
            fonts.Append(font2);

            Fills fills = new Fills() { Count = (UInt32Value)5U };

            // FillId = 0
            Fill fill1 = new Fill();
            PatternFill patternFill1 = new PatternFill() { PatternType = PatternValues.None };
            fill1.Append(patternFill1);

            // FillId = 1
            Fill fill2 = new Fill();
            PatternFill patternFill2 = new PatternFill() { PatternType = PatternValues.Gray125 };
            fill2.Append(patternFill2);

            // FillId = 2,PINK
            Fill fill3 = new Fill();
            PatternFill patternFill3 = new PatternFill() { PatternType = PatternValues.Solid };
            ForegroundColor foregroundColor1 = new ForegroundColor() { Rgb = HexBinaryValue.FromString("FA93E7") };
            BackgroundColor backgroundColor1 = new BackgroundColor() { Indexed = (UInt32Value)64U };
            patternFill3.Append(foregroundColor1);
            patternFill3.Append(backgroundColor1);
            fill3.Append(patternFill3);

            fills.Append(fill1);
            fills.Append(fill2);
            fills.Append(fill3);


            Borders borders = new Borders() { Count = (UInt32Value)1U };

            Border border1 = new Border();
            LeftBorder leftBorder1 = new LeftBorder();
            RightBorder rightBorder1 = new RightBorder();
            TopBorder topBorder1 = new TopBorder();
            BottomBorder bottomBorder1 = new BottomBorder();
            DiagonalBorder diagonalBorder1 = new DiagonalBorder();

            border1.Append(leftBorder1);
            border1.Append(rightBorder1);
            border1.Append(topBorder1);
            border1.Append(bottomBorder1);
            border1.Append(diagonalBorder1);

            borders.Append(border1);

            CellStyleFormats cellStyleFormats1 = new CellStyleFormats() { Count = (UInt32Value)1U };
            CellFormat cellFormat1 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U };

            cellStyleFormats1.Append(cellFormat1);

            CellFormats cellFormats = new CellFormats() { Count = (UInt32Value)4U };
            CellFormat cellFormat2 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)0U };
            CellFormat cellFormat3 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)1U, FillId = (UInt32Value)2U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)0U, ApplyFill = true };


            cellFormats.Append(cellFormat2);
            cellFormats.Append(cellFormat3);

            CellStyles cellStyles = new CellStyles() { Count = (UInt32Value)1U };
            CellStyle cellStyle1 = new CellStyle() { Name = "Normal", FormatId = (UInt32Value)0U, BuiltinId = (UInt32Value)0U };

            cellStyles.Append(cellStyle1);
            DifferentialFormats differentialFormats1 = new DifferentialFormats() { Count = (UInt32Value)0U };
            TableStyles tableStyles = new TableStyles() { Count = (UInt32Value)0U, DefaultTableStyle = "TableStyleMedium2", DefaultPivotStyle = "PivotStyleMedium9" };

            StylesheetExtensionList stylesheetExtensionList1 = new StylesheetExtensionList();

            StylesheetExtension stylesheetExtension1 = new StylesheetExtension() { Uri = "{EB79DEF2-80B8-43e5-95BD-54CBDDF9020C}" };
            stylesheetExtension1.AddNamespaceDeclaration("x14", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/main");
            X14.SlicerStyles slicerStyles1 = new X14.SlicerStyles() { DefaultSlicerStyle = "SlicerStyleLight1" };

            stylesheetExtension1.Append(slicerStyles1);

            stylesheetExtensionList1.Append(stylesheetExtension1);

            styleSheet.Append(fonts);
            styleSheet.Append(fills);
            styleSheet.Append(borders);
            styleSheet.Append(cellStyleFormats1);
            styleSheet.Append(cellFormats);
            styleSheet.Append(cellStyles);
            styleSheet.Append(differentialFormats1);
            styleSheet.Append(tableStyles);
            styleSheet.Append(stylesheetExtensionList1);
            return styleSheet;
        }

        //Method to validate the GSAP data
        void ValidateData(int id, SDxData sDxData, ILogger logger)
        {
            //SpreadsheetDocument spreadsheetDocument = null;
            WorksheetPart worksheetPart = null;
            List<GsaporiginalData> data = null;
            List<CsvColMapper> columns = null;
            JobDetails jd = null;
            string fileName = "UploadedFiles/" + id.ToString() + "/Reports/Floc-Error/floc_error_report_JobID_" + id.ToString() + ".xlsx";
            try
            {
                using MemoryStream ms = new MemoryStream();
                SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);

                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                //workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

                worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                //worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(new SheetData());
                Workbook workbook = new Workbook();
                FileVersion fileVersion = new FileVersion
                {
                    ApplicationName = "Microsoft Office Excel"
                };

                Worksheet worksheet = new Worksheet();
                WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                workbookStylesPart.Stylesheet = CreateStylesheet();
                workbookStylesPart.Stylesheet.Save();

                SheetData sheetData = new SheetData();

                try
                {
                    //Reading the data from the database
                    jd = dbContext.JOB_DETAILS.Where(x => x.JobId == id).First();
                    data = dbContext.GSAP_ORIGINAL_DATA.Where(x => x.JobId == id).ToList();
                    columns = dbContext.CSV_COL_MAPPER.Where(x => x.CsvName == "floc_error_report").OrderBy(x => x.CsvColSequence).ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("Could not read data from database " + e.Message);
                }

                //Adding the columns headers
                Row r = new Row();
                foreach (var colName in columns)
                {
                    Cell c = new Cell()
                    {
                        CellValue = new CellValue(colName.CsvColName),
                        DataType = CellValues.String,
                        StyleIndex = 0
                    };
                    r.Append(c);
                }
                sheetData.Append(r);

                char[] reference = "IJK".ToCharArray();
                int row = 2;

                if (data != null)
                {
                    logger.LogInformation("FLOC data validation started...");
                    jd.ProgressPercentage = 40;
                    //Iterating through each functional location of GSAP data
                    foreach (var record in data)
                    {
                        if (record.ActualLevel != 0)
                            continue;
                        List<string> hierarchy = new List<string>();
                        var floc = record;
                        hierarchy.Add(floc.FunctionalLocation);

                        //Iterating to find the actual floc level of the functional location and its hierarchy
                        while (floc.SuperiorFunctionalLocation != "")
                        {
                            floc = data.Where(x => x.FunctionalLocation == floc.SuperiorFunctionalLocation).SingleOrDefault();
                            hierarchy.Add(floc.FunctionalLocation);
                        }

                        //Validating each floc in the hierarchy
                        for (int i = 0; i < hierarchy.Count; i++)
                        {
                            var flocRecord = data.Where(x => x.FunctionalLocation == hierarchy[i]).FirstOrDefault();
                            if (flocRecord.ActualLevel == 0)
                            {
                                flocRecord.ActualLevel = hierarchy.Count - i;

                                //Calling the method to validate the floc level
                                string level = "L" + (hierarchy.Count - i).ToString();
                                bool valid = Valid(flocRecord, level);

                                if (valid == false)
                                {
                                    //Error in floc level
                                    flocRecord.HasError = true;
                                    if (hierarchy.Count < 5)
                                    {
                                        flocRecord.ErrorMessage = "Level in GSAP is " + flocRecord.SortField + ", actual level is " + level;
                                    }
                                    else
                                    {
                                        flocRecord.ErrorMessage = "Level is greater than 4";
                                    }

                                    //Marking all child flocs as erroneous if the current floc has error
                                    if (i > 0)
                                    {
                                        floc = record;
                                        for (int j = 0; j < i; j++)
                                        {
                                            floc.SuperiorFlocHasError = true;
                                            floc = data.Where(x => x.FunctionalLocation == floc.SuperiorFunctionalLocation).SingleOrDefault();
                                        }
                                    }
                                }
                                else
                                {
                                    flocRecord.HasError = false;
                                    flocRecord.SuperiorFlocHasError = false;
                                    flocRecord.ErrorMessage = null;
                                }

                                //Checking if the record is new or existing if it is floc 1
                                if (flocRecord.ActualLevel == 1)
                                {
                                    var pbsExtract = sDxData.Floc1Data.Where(x => x.Name == flocRecord.FunctionalLocation).FirstOrDefault();
                                    if (pbsExtract != null)
                                        flocRecord.IsNewRecord = false;
                                    else
                                    {
                                        flocRecord.IsNewRecord = true;
                                        flocRecord.HasError = true;
                                        flocRecord.ErrorMessage = "No country found for Terminal";
                                    }
                                }
                                else
                                //Checking if the record is new or existing for floc 2, floc 3, floc 4
                                {
                                    flocRecord.IsNewRecord = true;
                                    if ((hierarchy.Count - i) == 2 && sDxData.Floc2Data.Where(x => x.Name == flocRecord.FunctionalLocation).FirstOrDefault() != null)
                                        flocRecord.IsNewRecord = false;
                                    if ((hierarchy.Count - i) == 3 && sDxData.Floc3Data.Where(x => x.Name == flocRecord.FunctionalLocation).FirstOrDefault() != null)
                                        flocRecord.IsNewRecord = false;
                                    if ((hierarchy.Count - i) == 4 && sDxData.Floc4Data.Where(x => x.Name == flocRecord.FunctionalLocation).FirstOrDefault() != null)
                                        flocRecord.IsNewRecord = false;

                                    /*string column = "FlocLevel" + (hierarchy.Count - i).ToString();

                                    PropertyInfo prop = type.GetProperty(column);
                                    var pbsExtract = extract.Where(x => prop.GetValue(x).ToString() == flocRecord.FunctionalLocation).FirstOrDefault();
                                    if (pbsExtract != null)
                                        flocRecord.IsNewRecord = false;                                
                                    else
                                        flocRecord.IsNewRecord = true;*/
                                }

                            }
                            else
                            {
                                if (flocRecord.HasError == true)
                                {
                                    //Marking all child flocs as erroneous if the current floc has error
                                    if (i > 0)
                                    {
                                        floc = record;
                                        for (int j = 0; j < i; j++)
                                        {
                                            floc.SuperiorFlocHasError = true;
                                            floc = data.Where(x => x.FunctionalLocation == floc.SuperiorFunctionalLocation).SingleOrDefault();
                                        }
                                    }
                                }
                            }

                        }

                        //Writing the floc hierarchy into the excel file
                        r = new Row();
                        int colIndex = 0;
                        string HasError = "No";
                        string IsNewFloc = "No";
                        string errorMessage = "";

                        for (int i = hierarchy.Count - 1; i >= 0; i--)
                        {
                            floc = data.Where(x => x.FunctionalLocation == hierarchy[i]).FirstOrDefault();
                            Cell cell1 = new Cell()
                            {
                                CellValue = new CellValue(floc.FunctionalLocation),
                                DataType = CellValues.String,
                                StyleIndex = 0
                            };
                            if (floc.HasError == true)
                            {
                                cell1.StyleIndex = 1;
                                var headerRow = sheetData.FirstChild;
                                Cell headCell = (Cell)headerRow.ChildElements[colIndex];
                                headCell.StyleIndex = 1;

                            }

                            /*if (floc.IsNewRecord == true && floc.ActualLevel != 1)
                                cell1.StyleIndex = 1;*/
                            r.Append(cell1);
                            Cell cell2 = new Cell()
                            {
                                CellValue = new CellValue(floc.DescriptionFunctionLocation),
                                DataType = CellValues.String,
                                StyleIndex = 0
                            };
                            r.Append(cell2);

                            if (floc.HasError == true)
                            {
                                HasError = "Yes";
                                errorMessage = errorMessage + "Error: Floc " + floc.ActualLevel + "-" + floc.ErrorMessage + "\r\n";
                            }

                            if (floc.IsNewRecord == true)
                            {
                                IsNewFloc = "Yes";
                                if (floc.ActualLevel != 1)
                                    errorMessage = errorMessage + "Info: Floc " + floc.ActualLevel + "-" + "New record found\r\n";
                            }

                            colIndex += 2;
                        }
                        //Writing if the floc has error and the error message into the excel file
                        Cell dataCell = new Cell()
                        {
                            CellValue = new CellValue(HasError),
                            DataType = CellValues.String,
                            CellReference = reference[0].ToString() + row,
                            StyleIndex = 0
                        };
                        r.Append(dataCell);
                        dataCell = new Cell()
                        {
                            CellValue = new CellValue(IsNewFloc),
                            DataType = CellValues.String,
                            CellReference = reference[1].ToString() + row,
                            StyleIndex = 0
                        };
                        r.Append(dataCell);
                        dataCell = new Cell()
                        {
                            CellValue = new CellValue(errorMessage),
                            DataType = CellValues.String,
                            CellReference = reference[2].ToString() + row,
                            StyleIndex = 0
                        };
                        r.Append(dataCell);

                        sheetData.Append(r);
                        row++;

                    }

                    worksheet.Append(sheetData);
                    worksheetPart.Worksheet = worksheet;
                    worksheetPart.Worksheet.Save();
                    Sheets sheets = new Sheets();
                    Sheet sheet = new Sheet()
                    {
                        Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "FLOC_ERROR_REPORT"
                    };
                    sheets.Append(sheet);
                    workbook.Append(fileVersion);
                    workbook.Append(sheets);

                    spreadsheetDocument.WorkbookPart.Workbook = workbook;
                    spreadsheetDocument.WorkbookPart.Workbook.Save();
                    spreadsheetDocument.Save();
                    spreadsheetDocument.Close();
                    ms.Position = 0;
                    string filepath = blobStorageService.UploadFileToBlob(fileName, ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");


                    /*worksheetPart.Worksheet.Save();
                    spreadsheetDocument.Close();*/
                    logger.LogInformation("Floc Error Report generated...");
                    jd.ProgressPercentage = 50;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();

                    //Updating the record in GSAP_ORIGINAL_DATA
                    try
                    {
                        dbContext.BulkUpdate(data);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Could not update validated GSAP in database: " + e.Message);
                    }

                    logger.LogInformation("Data validation completed");
                    jd.ProgressPercentage = 55;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();

                    //Segregating records according to the floc level and inserting it to the respective tables and generating the CSVs                
                    WriteFloc1Details(data, sDxData.Floc1Data, id);
                    WriteFloc2Details(data, id);
                    WriteFloc3Details(data, id);
                    WriteFloc4Details(data, id);
                    logger.LogInformation("FLOCs written to database and CSVs generated");
                    jd.ProgressPercentage = 60;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();

                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not validate GSAP data: " + e.Message);
            }
            try
            {
                List<Datacharts> summaryData = new List<Datacharts>();

                for (int i = 1; i <= 4; i++)
                {
                    summaryData.Add(GetFlocSummary(data, i, id));
                }
                dbContext.BulkInsert(summaryData);
            }
            catch (Exception e)
            {
                throw new Exception("Could not insert floc summary details into database: " + e.Message);
            }
        }

        //Method to validate the equipment data from GSAP
        void ValidateEquipmentData(int id, List<PbsObject> existingEquipment, List<PbsObject> technicalObjectTypeData, ILogger logger)
        {
            string path = "UploadedFiles/" + id.ToString() + "/Reports/Equipment-Error/equipment_error_report_JobID_" + id.ToString() + ".xlsx";
            List<EquipmentDataFromGsap> data = null;
            List<CsvColMapper> columns = null;
            List<string> technicalObjects = null;
            List<Floc4> Floc4Data = null;
            JobDetails jd = null;
            //SpreadsheetDocument spreadsheetDocument = null;
            WorksheetPart worksheetPart = null;
            try
            {
                try
                {
                    jd = dbContext.JOB_DETAILS.Where(x => x.JobId == id).First();
                    //Reading Equipment data from the database
                    data = dbContext.EQUIPMENT_DATA_FROM_GSAP.Where(x => x.JobId == id).ToList();
                    technicalObjects = technicalObjectTypeData.Select(x => x.Name).ToList();
                    columns = dbContext.CSV_COL_MAPPER.Where(x => x.DbTableName == "EQUIPMENT_DATA_FROM_GSAP").OrderBy(x => x.CsvColSequence).ToList();
                    Floc4Data = dbContext.FLOC_4_DETAILS.Where(x => x.JobId == id).ToList();

                }
                catch (Exception e)
                {
                    throw new Exception("Could not get equipment related data: " + e.Message);
                }

                int flocColumn = columns.Where(x => x.DbColName == "FunctionalLocation").Select(x => x.CsvColSequence).FirstOrDefault();
                int totColumn = columns.Where(x => x.DbColName == "TechnicalObjectType").Select(x => x.CsvColSequence).FirstOrDefault();
                int sapIdColumn = columns.Where(x => x.DbColName == "SapId").Select(x => x.CsvColSequence).FirstOrDefault();

                //Creating a new excel workbook
                using MemoryStream ms = new MemoryStream();
                SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);

                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                //workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

                worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                //worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(new SheetData());
                Workbook workbook = new Workbook();
                FileVersion fileVersion = new FileVersion
                {
                    ApplicationName = "Microsoft Office Excel"
                };

                Worksheet worksheet = new Worksheet();
                WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                workbookStylesPart.Stylesheet = CreateStylesheet();
                workbookStylesPart.Stylesheet.Save();

                SheetData sheetData = new SheetData();

                Row r = new Row();
                foreach (var colName in columns)
                {
                    Cell cell = new Cell()
                    {
                        CellValue = new CellValue(colName.CsvColName),
                        DataType = CellValues.String,
                        StyleIndex = 0
                    };

                    r.Append(cell);
                }

                Cell c = new Cell()
                {
                    CellValue = new CellValue("Has Error"),
                    DataType = CellValues.String,
                    StyleIndex = 0
                };
                r.Append(c);

                c = new Cell()
                {
                    CellValue = new CellValue("New Record"),
                    DataType = CellValues.String,
                    StyleIndex = 0
                };
                r.Append(c);

                c = new Cell()
                {
                    CellValue = new CellValue("Error Message / Comments"),
                    DataType = CellValues.String,
                    StyleIndex = 0
                };
                r.Append(c);

                sheetData.Append(r);

                var headerRow = sheetData.FirstChild;

                Cell FlocHeaderCell = (Cell)headerRow.ChildElements[flocColumn - 1];
                Cell SapHeaderCell = (Cell)headerRow.ChildElements[sapIdColumn - 1];
                Cell ObjTypeHeaderCell = (Cell)headerRow.ChildElements[totColumn - 1];

                if (data != null)
                {
                    logger.LogInformation("Equipment data validation started ");
                    jd.ProgressPercentage = 70;
                    foreach (var equip in data)
                    {
                        Floc4 result = null;
                        r = new Row();
                        string errorMessage = "";
                        string IsError = "No";

                        equip.IsNewRecord = false;
                        equip.FlocHasError = false;
                        equip.SapHasError = false;
                        equip.ObjTypeHasError = false;

                        //Checking if the functional location of the equipment is valid or not
                        try
                        {
                            result = Floc4Data.Where(f => f.FunctionalLocation == equip.FunctionalLocation).FirstOrDefault();
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Could not get FLOC data to validate equipment: " + e.Message);
                        }

                        if (result == null)
                        {
                            //Invalid
                            equip.FlocHasError = true;
                            IsError = "Yes";
                            FlocHeaderCell.StyleIndex = 1;
                            errorMessage = errorMessage + "Error: Floc-Record for Floc does not exist" + Environment.NewLine;
                        }

                        if (equip.SapId == "" || !(equip.SapId.Substring(0, 6).Equals("000000") && equip.Equipment.Equals(equip.SapId[6..])))
                        {
                            equip.SapHasError = true;
                            IsError = "Yes";
                            SapHeaderCell.StyleIndex = 1;
                            errorMessage = errorMessage + "Error: SAP ID-SAP ID is invalid" + Environment.NewLine;
                        }

                        if (!technicalObjects.Contains(equip.TechnicalObjectType))
                        {
                            equip.ObjTypeHasError = true;
                            IsError = "Yes";
                            ObjTypeHeaderCell.StyleIndex = 1;
                            errorMessage = errorMessage + "Error: Technical Object Type-Technical object type does not exist in SDx" + Environment.NewLine;
                        }

                        //Checking if equipment is new or old record
                        var res = existingEquipment.Where(x => x.Name == equip.Equipment).FirstOrDefault();
                        if (res == null)
                        {
                            equip.IsNewRecord = true;
                            errorMessage = errorMessage + "Info: Equipment-New Record found";
                        }

                        equip.ErrorMessage = errorMessage;


                        foreach (var column in columns)
                        {
                            Cell cell = new Cell()
                            {
                                CellValue = new CellValue(equip.GetType().GetProperty(column.DbColName).GetValue(equip).ToString()),
                                DataType = CellValues.String,
                                StyleIndex = 0
                            };

                            if (column.CsvColSequence == flocColumn && equip.FlocHasError == true)
                            {
                                cell.StyleIndex = 1;
                                r.Append(cell);
                                continue;
                            }

                            if (column.CsvColSequence == sapIdColumn && equip.SapHasError == true)
                            {
                                cell.StyleIndex = 1;
                                r.Append(cell);
                                continue;

                            }
                            if (column.CsvColSequence == totColumn && equip.ObjTypeHasError == true)
                            {
                                cell.StyleIndex = 1;
                                r.Append(cell);
                                continue;
                            }
                            r.Append(cell);

                        }
                        c = new Cell()
                        {
                            CellValue = new CellValue(IsError),
                            DataType = CellValues.String,
                            StyleIndex = 0
                        };
                        r.Append(c);

                        var NewRecord = (bool)equip.GetType().GetProperty("IsNewRecord").GetValue(equip) ? "Yes" : "No";
                        c = new Cell()
                        {
                            CellValue = new CellValue(NewRecord),
                            DataType = CellValues.String,
                            StyleIndex = 0
                        };
                        r.Append(c);

                        c = new Cell()
                        {
                            CellValue = new CellValue(errorMessage),
                            DataType = CellValues.String,
                            StyleIndex = 0
                        };
                        r.Append(c);

                        sheetData.Append(r);

                        /*if (data.IndexOf(equip) % 1000 == 0)
                            Console.WriteLine("Equipment " + data.IndexOf(equip) + " validated at " + DateTime.Now);*/
                    }

                    worksheet.Append(sheetData);
                    worksheetPart.Worksheet = worksheet;
                    worksheetPart.Worksheet.Save();
                    Sheets sheets = new Sheets();
                    Sheet sheet = new Sheet()
                    {
                        Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "EQUIPMENT_ERROR_REPORT"
                    };
                    sheets.Append(sheet);
                    workbook.Append(fileVersion);
                    workbook.Append(sheets);

                    spreadsheetDocument.WorkbookPart.Workbook = workbook;
                    spreadsheetDocument.WorkbookPart.Workbook.Save();
                    spreadsheetDocument.Save();
                    spreadsheetDocument.Close();

                    ms.Position = 0;
                    string filepath = blobStorageService.UploadFileToBlob(path, ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");


                    /*worksheetPart.Worksheet.Save();
                    spreadsheetDocument.Close();*/
                    logger.LogInformation("Equipment error report generated");
                    jd.ProgressPercentage = 80;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();


                    //Updating the error status in the database table
                    dbContext.BulkUpdate(data);
                    logger.LogInformation("Equipments validation completed");
                    jd.ProgressPercentage = 95;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();


                    //Calling the method to write the valid data into a separate table
                    WriteEquipmentDetails(id);
                    logger.LogInformation("Equipments written to database and CSV generated");
                    jd.ProgressPercentage = 99;
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();


                }

            }
            catch (Exception e)
            {
                throw new Exception("Could not validate equipment data: " + e.Message);
            }
            try
            {
                Datacharts summary = new Datacharts
                {
                    JobId = id,
                    PbsType = "Equipment",
                    TotalRecords = data.Count(),
                    Errors = data.Where(x => x.FlocHasError == true || x.SapHasError == true || x.ObjTypeHasError == true).Count(),
                    NewRecords = data.Where(x => x.IsNewRecord == true).Count()

                };
                summary.ImpactedRecords = summary.Errors;
                summary.ErrorPercentage = Math.Round((decimal)summary.Errors / summary.TotalRecords * 100, 5);
                summary.ImpactedPercentage = Math.Round((decimal)summary.ImpactedRecords / summary.TotalRecords * 100, 5);
                summary.NewRecordPercentage = Math.Round((decimal)summary.NewRecords / summary.TotalRecords * 100, 5);

                dbContext.JOB_SUMMARY.Add(summary);
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                throw new Exception("Could not insert equipment summary details into database: " + e.Message);
            }
        }

        //Action to validate data

        public async Task ValidateGsapDataAsync(RequestData data, ILogger logger)
        {
            //Id = id;

            JobDetails jd = dbContext.JOB_DETAILS.Where(x => x.JobId == data.JobId).FirstOrDefault();
            bool dataIsReady = false;
            try
            {

                logger.LogInformation("Reading data from SDx...(May take a few minutes)");
                jd.Status = "Retrieving data from SDx";
                jd.ProgressPercentage = 10;
                dbContext.JOB_DETAILS.Update(jd);
                dbContext.SaveChanges();
                Task<List<Floc1Data>> F1 = GetFloc1DetailsFromSDx("Subunits");
                Task<List<PbsObject>> F2 = GetFlocDetailsFromSDx("floc2s");
                Task<List<PbsObject>> F3 = GetFlocDetailsFromSDx("floc3s");
                Task<List<PbsObject>> F4 = GetFlocDetailsFromSDx("FLOCs");
                Task<List<PbsObject>> Equipment = GetFlocDetailsFromSDx("Assets");
                Task<List<PbsObject>> TechinalObjType = GetTechnicalObjTypeDataFromSDx();

                await Task.WhenAll(F1, F2, F3, F4, Equipment, TechinalObjType);
                logger.LogInformation("Retrieved data from SDx");
                jd.ProgressPercentage = 25;

                SDxData sDxData = new SDxData
                {
                    Floc1Data = F1.Result,
                    Floc2Data = F2.Result,
                    Floc3Data = F3.Result,
                    Floc4Data = F4.Result
                };

                List<PbsObject> equipmentData = Equipment.Result;
                List<PbsObject> technicalObjTypeData = TechinalObjType.Result;

                //Calling the method to read CSV data            
                //logger.LogInformation("Started reading CSVs at " + DateTime.Now;
                jd.Status = "Reading data from files";
                dbContext.JOB_DETAILS.Update(jd);
                dbContext.SaveChanges();
                dataIsReady = ReadCSV(data.JobId, data.GsapFilePath, data.EquipmentFilePath);

                logger.LogInformation("Completed reading CSVs...");
                jd.ProgressPercentage = 35;

                //Data successfully read from CSV
                if (dataIsReady == true)
                {
                    jd.Status = "Validating Floc Data";
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();
                    //Calling the methods to validate the data
                    ValidateData(data.JobId, sDxData, logger);

                    jd.Status = "Validating Equipment Data";
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();
                    ValidateEquipmentData(data.JobId, equipmentData, technicalObjTypeData, logger);
                    jd.Status = "Equipment Data Validation Completed";
                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();

                    //Saving status as validation is successfully completed
                    jd.Status = "Validation Completed";
                    jd.ProgressPercentage = 100;
                    stopWatch.Stop();
                    var executionTime = stopWatch.Elapsed;
                    jd.TimeTaken = executionTime.Hours + " Hrs " + executionTime.Minutes + " mins " + executionTime.Seconds + " seconds";

                    dbContext.JOB_DETAILS.Update(jd);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("summary"))
                {
                    jd.Status = "Validation Completed";
                    jd.ProgressPercentage = 100;
                    stopWatch.Stop();
                    var executionTime = stopWatch.Elapsed;
                    jd.TimeTaken = executionTime.Hours + " Hrs " + executionTime.Minutes + " mins " + executionTime.Seconds + " seconds";
                }
                else
                {
                    jd.Status = "Validation Failed";
                    jd.ErrorMessage = e.Message;
                }
                dbContext.JOB_DETAILS.Update(jd);
                dbContext.SaveChanges();
            }
            //return RedirectToAction("Create", "AddJob");
        }

        Datacharts GetFlocSummary(List<GsaporiginalData> details, int level, int jobId)
        {
            Datacharts data = new Datacharts
            {
                JobId = jobId,
                PbsType = "Floc " + level.ToString(),
                TotalRecords = details.Where(x => x.ActualLevel == level).Count(),
                Errors = details.Where(x => x.ActualLevel == level && x.HasError == true).Count(),
                ImpactedRecords = details.Where(x => x.ActualLevel == level && x.HasError == false && x.SuperiorFlocHasError == true).Count(),
                NewRecords = details.Where(x => x.ActualLevel == level && x.IsNewRecord == true).Count()

            };

            data.ErrorPercentage = Math.Round((decimal)data.Errors / data.TotalRecords * 100, 5);
            data.ImpactedPercentage = Math.Round((decimal)data.ImpactedRecords / data.TotalRecords * 100, 5);
            data.NewRecordPercentage = Math.Round((decimal)data.NewRecords / data.TotalRecords * 100, 5);
            return data;
        }
    }
}
