using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Logics
{
    public static class EncryptedMLHelper
    {
        static public List<List<Ciphertext>> extract2DCipherListFromStream(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){

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

        static public long[] convert2DCipherListToStream(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){

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

        static public List<List<float>> ReadValuesToList(string csvPath)
        {
            List<List<float>> list = new List<List<float>>();
            using StreamReader reader = new StreamReader(csvPath);
            while (!reader.EndOfStream)
            {
                String line = reader.ReadLine();
                float[] values = Array.ConvertAll(line.Split(','), float.Parse);
                List<float> featureList = new List<float>();
                foreach (float value in values) featureList.Add(value);
                list.Add(featureList);
            }

            return list;
        }

        static public List<List<Ciphertext>> encryptValues(List<List<float>> featureList,  PublicKey pk, SEALContext context)
        {

            IntegerEncoder encoder = new IntegerEncoder(context);
            Encryptor encryptor = new Encryptor(context, pk);

            List<List<Ciphertext>> cList = new List<List<Ciphertext>>();
            foreach (List<float> row in featureList)
            {
                 List<Ciphertext> cRow = new List<Ciphertext>();
                for (int i = 0; i < row.Count; i++)
                {
                    long featureItem = (long)(row[i] * 1000);
                    Plaintext fPlain = encoder.Encode(featureItem);
                    Ciphertext fCipher = new Ciphertext();

                    encryptor.Encrypt(fPlain, fCipher);

                    cRow.Add(fCipher);
                }
                cList.Add(cRow);
            }

            return cList;
        }

        static public List<List<long>> decryptValues(List<List<Ciphertext>> weihtedSums,  SecretKey sk, SEALContext context)
        {

             List<List<long>> results = new List<List<long>>();

            Decryptor decryptor = new Decryptor(context, sk);
            IntegerEncoder encoder = new IntegerEncoder(context);

            int sample_ind = 0;
            foreach (List<Ciphertext> sample in weihtedSums)
            {
                List<long> resultsRow = new List<long>();
                foreach (Ciphertext encryptedClassScore in sample)
                {
                    Plaintext plainClassScore = new Plaintext();

                    if (decryptor.InvariantNoiseBudget(encryptedClassScore) == 0){
                        throw new Exception("Noise budget depleated in sample "+sample_ind+". Aborting...");
                    }
                    decryptor.Decrypt(encryptedClassScore, plainClassScore);
                    long ClassScore = encoder.DecodeInt64(plainClassScore)/(1000*1000);
                    
                    resultsRow.Add(ClassScore);
                }
                results.Add(resultsRow);
                sample_ind++;
            }

            return results;
        }
    
    }
}