using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Research.SEAL;
using System.IO;
using System.Threading.Tasks; 
using System.Runtime.Serialization.Formatters.Binary;

using CDTS_PROJECT.Models;
using CDTS_PROJECT.Services;
using CDTS_PROJECT.Logics;
using CDTS_PROJECT.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace CDTS_PROJECT.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EncryptedMLController : ControllerBase
    {

        private readonly ILogger<EncryptedMLController> _logger;
        private readonly IModelService _modelService;
        private readonly IencryptedOperationsService _encryptedOperationsService;
        private readonly IContextManager _contextManager;

        public EncryptedMLController(ILogger<EncryptedMLController> logger, IModelService modelService, IencryptedOperationsService encryptedOperationsService, IContextManager contextManager)
        {
            _logger = logger;
            _modelService = modelService;
            _encryptedOperationsService = encryptedOperationsService;
            _contextManager = contextManager;
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> GetWeightedSums([FromQuery] string modelname) //return type will be List<List<Ciphertext>> which will be automaticall serialized into JSON in response
        {   

            if (! EncryptedMLHelper.validateModelName(modelname)){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400;
                errorResponse.Value = "Model name should contain only alphanumeric characters.";
                throw errorResponse;
            }

            //load chosen model
            Model selectedModel = _modelService.Get(modelname);

            if (selectedModel == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 404;
                errorResponse.Value = "Model with name "+modelname+" does not exist.";
                throw errorResponse;
            }

            //parse content stream into parts
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = EncryptedMLHelper.GetBoundary( MediaTypeHeaderValue.Parse(Request.ContentType));
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            //Use first part to initialize public key
            //var section = await reader.ReadNextSectionAsync();
            //if (section == null){
            //    HttpResponseException errorResponse =  new HttpResponseException();
            //    errorResponse.Status = 400;
            //    errorResponse.Value = "0 content parts received, expected 3";
            //    throw errorResponse;
            //}
            //MemoryStream tempStream = new MemoryStream();
            //await section.Body.CopyToAsync(tempStream);
            //tempStream.Seek(0, SeekOrigin.Begin);
            //PublicKey publicKey = new PublicKey();
            //try{
            //    publicKey.Load(_contextManager.Context, tempStream);
            //}catch (Exception ex){
            //    HttpResponseException errorResponse =  new HttpResponseException();
            //    errorResponse.Status = 400;
            //    errorResponse.Value = "Public Key Content Stream May be Corrupted. Error extracting public key." + ex.ToString();
            //    throw errorResponse;
            //}
            //tempStream.Close();

            //Use first part to initialize encryptedFeatureValues
            var section = await reader.ReadNextSectionAsync();
            if (section == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400;
                errorResponse.Value = "0 content parts received, expected 2";
                throw errorResponse;
            }
            MemoryStream tempStream = new MemoryStream();
            await section.Body.CopyToAsync(tempStream);
            tempStream.Seek(0, SeekOrigin.Begin);

            //Use second part to initialize columnSizes
            section = await reader.ReadNextSectionAsync();
            if (section == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400;
                errorResponse.Value = "0 content parts received, expected 1";
                throw errorResponse;
            }
            MemoryStream tempStream2 = new MemoryStream();
            await section.Body.CopyToAsync(tempStream2);
            tempStream2.Seek(0, SeekOrigin.Begin);
            BinaryFormatter formatter = new BinaryFormatter();

            List<long> columnSizes = new List<long>();  
            try{
                long[] columnSizesArray = (long[]) formatter.Deserialize(tempStream2);
                columnSizes.AddRange(columnSizesArray);
            }catch (Exception ex){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400;
                errorResponse.Value = "Column Sizes Content Stream May be Corrupted. Error Deserializing 2nd content part: "+ex.ToString();
                throw errorResponse;
            }
            tempStream2.Close();
            
            //extract encryptedFeatureValues from stream
            List<List<Ciphertext>> encryptedFeatureValues = new List<List<Ciphertext>>();
            try{
                encryptedFeatureValues = await EncryptedMLHelper.extract2DCipherListFromStreamAsync(tempStream, columnSizes.ToArray(), _contextManager.Context);
            }catch (Exception ex){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400;
                errorResponse.Value = "Encrypted Feature Content Stream Values May Be Corrupted. Error extracting encryptedfeatures from content stream: "+ex.ToString();
                throw errorResponse;
            }
            tempStream.Close();

            //create query
            Query query = new Query{
                //publicKey = publicKey,
                encryptedFeatureValues = encryptedFeatureValues
            };

            List<List<Ciphertext>> encryptedWeightedSums = new List<List<Ciphertext>>();
            try{
                encryptedWeightedSums = await _encryptedOperationsService.calculateWeightedSumAsync(selectedModel, query);
            }catch (Exception ex){
                throw ex;
            }

            //serialize encryptedFeatureValues
            MemoryStream encryptedFeatureValuesStream = new MemoryStream();
            long[] weightSumSizes = await EncryptedMLHelper.convert2DCipherListToStreamAsync(encryptedWeightedSums, encryptedFeatureValuesStream);
            UInt64 sizeOfEncryptedFeatureValues = (UInt64)(encryptedFeatureValuesStream.Length);
            
            //serialize columnSizes (list of the sizes of each ciphertext object in query.encryptedFeatureValues)
            MemoryStream columnSizesStream = new MemoryStream();
            formatter.Serialize(encryptedFeatureValuesStream, weightSumSizes); //copy columnSizes to end of encryptedFeatureValuesStream
            
            // copy total size of the encryptedFeatureValues Cipher objects to the end of encryptedFeatureValuesStream
            BinaryWriter writer = new BinaryWriter(encryptedFeatureValuesStream);
            writer.Seek(0, SeekOrigin.End);
            writer.Write(sizeOfEncryptedFeatureValues);
            
            //create response and attach encryptedFeatureValuesStream
            encryptedFeatureValuesStream.Seek(0, SeekOrigin.Begin);

            return File(encryptedFeatureValuesStream, "application/octet-stream");;
        }
    }
}
