using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CDTS_PROJECT.Services;
using System.Collections.Generic;
using CDTS_PROJECT.Models;
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;
using System.Threading.Tasks;
using CDTS_PROJECT.Exceptions;

namespace CDTS_PROJECT.Services
{

    public interface IencryptedOperationsService
    {
        List<List<Ciphertext>> calculateWeightedSum(Model selectedModel, Query query); 

        Task<List<List<Ciphertext>>> calculateWeightedSumAsync(Model selectedModel, Query query);

    }

    public class encryptedOperationsService : IencryptedOperationsService
    {
        private readonly ILogger<encryptedOperationsService> _logger;

        private readonly IContextManager _contextManager;

        public encryptedOperationsService(ILogger<encryptedOperationsService> logger, IContextManager ContextManager)
        {
            _logger = logger;
            _contextManager = ContextManager;
        }


        public async Task<List<List<Ciphertext>>> calculateWeightedSumAsync(Model selectedModel, Query query){
            return await Task.Run(() => calculateWeightedSum(selectedModel, query));
        }
        
        public List<List<Ciphertext>> calculateWeightedSum(Model selectedModel, Query query)
        {
            
            int precision = selectedModel.Precision;
            
            //extract public key and encryption parameters from query
            //PublicKey publicKey = query.publicKey;

            List<List<Ciphertext>> encryptFeatureValues = query.encryptedFeatureValues;

            //extract model weights from Model object
            List<double[]> weights = selectedModel.Weights;
            int N_features = selectedModel.N_weights;

            //initialize integer encoder, encryptor, and evaluator
            IntegerEncoder encoder = new IntegerEncoder(_contextManager.Context); 
            //Encryptor encryptor = new Encryptor(_contextManager.Context, publicKey);
            Evaluator evaluator = new Evaluator(_contextManager.Context);

            List<List<Ciphertext>> weightedSums = new List<List<Ciphertext>>(); //holds encrypted weighted sums for all classes for all samples
            
            int sampleIndex = 0;
            foreach (List<Ciphertext> sample in encryptFeatureValues){ //for each sample in encrypted values

                if(sample.Count != N_features){
                    HttpResponseException errorResponse =  new HttpResponseException();
                    errorResponse.Status = 404;
                    errorResponse.Value = "Sample "+sampleIndex.ToString()+" has "+sample.Count.ToString()+" features but "+N_features.ToString()+" expected";
                    throw errorResponse;  
                }

                List<Ciphertext> sampleWeightedSums = new List<Ciphertext>();  //holds encrypted weighted sums for all classes for this sample
                
                foreach(double[] classWeights in weights){ //for each class 

                    //for each sample, calculate the encrypted weighted feature value and store it in weightedFeatures
                    List<Ciphertext> weightedFeatures = new List<Ciphertext>();
                    for (int i = 0; i < sample.Count; i++){ 
                        
                        Ciphertext curFeature = sample[i];
                        long curWeight = (long)(classWeights[i] * precision);
                        
                        if (curWeight == 0){
                            continue;
                        }
                        
                        Plaintext scaledWeight = encoder.Encode(curWeight);
                        Ciphertext weightedFeature = new Ciphertext();
                        
                        evaluator.MultiplyPlain(curFeature, scaledWeight, weightedFeature);
                        
                        weightedFeatures.Add(weightedFeature);
                    }

                    //calculate encrypted weighted sum and append it to sampleWeightedSums
                    Ciphertext weightedSum = new Ciphertext();
                    evaluator.AddMany(weightedFeatures, weightedSum);
                    sampleWeightedSums.Add(weightedSum);
                    
                    //deallocate variables
                    weightedFeatures = null;
                    GC.Collect();
                }

                weightedSums.Add(sampleWeightedSums);

                sampleIndex ++;
            }

            return weightedSums; //replace with return weighted sums
        }

    }
}