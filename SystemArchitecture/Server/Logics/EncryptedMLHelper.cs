using System;
using System.IO;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.Research.SEAL;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CDTS_PROJECT.Logics
{
    public static class EncryptedMLHelper
    {
        public static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            return boundary;
        }

        public static bool validateModelName(string modelname){
            return Regex.IsMatch(modelname, "^[a-zA-Z0-9]+$");
        }

        public static async Task<List<List<Ciphertext>>> extract2DCipherListFromStreamAsync(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){
            return await Task.Run(() => extract2DCipherListFromStream(encryptedFeatureValuesStream, columnSizes, context));
        }

        public static List<List<Ciphertext>> extract2DCipherListFromStream(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){

            List<List<Ciphertext>> encryptedFeatureValues = new List<List<Ciphertext>>();

            int offset = 0;
            List<Ciphertext> curSample = new List<Ciphertext>();
            foreach (long size in columnSizes){

                if (size == -1){
                    encryptedFeatureValues.Add(curSample);
                    curSample = new List<Ciphertext>();
                    continue;
                }

                Byte[] tempByteArray = new byte[(int)size];
                encryptedFeatureValuesStream.Read(tempByteArray, 0, (int)size);
                MemoryStream tempStream = new MemoryStream(tempByteArray);
                tempStream.Seek(0, SeekOrigin.Begin);
                Ciphertext encryptedFeature = new Ciphertext();
                encryptedFeature.Load(context, tempStream);
                tempStream.Close();

                curSample.Add(encryptedFeature);
                
                offset += (int)size;
            }

            return encryptedFeatureValues;

        }


        public static async Task<long[]> convert2DCipherListToStreamAsync(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){
            return await Task.Run(() => convert2DCipherListToStream(encryptedFeatureValues, memoryStream));
        }
        public static long[] convert2DCipherListToStream(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){

            //initialize list of Ciphertext object sizes    
            long[] columnSizes = new long[encryptedFeatureValues.Count * (encryptedFeatureValues[0].Count + 1)]; 
            int curIndex = 0;
            
            long lastPosition = memoryStream.Position; 
            long curSize = 0;

            foreach (List<Ciphertext> sample in encryptedFeatureValues) //for each sample
            {
                foreach (Ciphertext encryptedFeature in sample)//for each feature
                {
                    
                    encryptedFeature.Save(memoryStream); //write current column (i.e. feature) to stream

                    //get size of the current column and append it to columnSizes
                    curSize = memoryStream.Position - lastPosition;
                    lastPosition = memoryStream.Position;
                    columnSizes[curIndex] = curSize;
                    curIndex++;
                }
                columnSizes[curIndex] = -1;
                curIndex++;
            }   


            return columnSizes;
        }
    }
}