using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks; 
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace CDTS_PROJECT
{

    public class Query{
        public PublicKey publicKey { get; set;}
        public List<List<Ciphertext>> encryptedFeatureValues { get; set;}
    }

    class Program
    {
        static ContextManager contextManager = new ContextManager();
        static KeyManager keyManager = new KeyManager(contextManager);
        static HttpClient client = new HttpClient(); //instantiate HttpClient

        static async Task<List<List<Ciphertext>>> getPredictions(Query query)
        {

            //serialized publicKey
            //MemoryStream pkStream = new MemoryStream();
            //query.publicKey.Save(pkStream);
            //pkStream.Seek(0, SeekOrigin.Begin);
            //StreamContent pkStreamContent = new StreamContent(pkStream);

            //serialize encryptedFeatureValues
            MemoryStream encryptedFeatureValuesStream = new MemoryStream();
            long[] columnSizes = Logics.EncryptedMLHelper.convert2DCipherListToStream(query.encryptedFeatureValues, encryptedFeatureValuesStream);
            encryptedFeatureValuesStream.Seek(0, SeekOrigin.Begin);
            StreamContent encryptedFeatureValuesStreamContent = new StreamContent(encryptedFeatureValuesStream);

            //serialize columnSizes (list of the sizes of each ciphertext object in query.encryptedFeatureValues)
            MemoryStream columnSizesStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(columnSizesStream, columnSizes);
            columnSizesStream.Seek(0, SeekOrigin.Begin);
            StreamContent columnSizesStreamContent = new StreamContent(columnSizesStream);

            //send encryptedFeatreValues and publicKey to server as multi part stream contents
            var content = new MultipartFormDataContent(); 
            //content.Add(pkStreamContent, "privateKey");
            content.Add(encryptedFeatureValuesStreamContent, "encryptedFeatureValues");
            content.Add(columnSizesStreamContent, "columnSizes");
            var response = await client.PostAsync("api/encryptedML?modelname=LogisticRegression", content); //model name will be selected by user in future versions

            if (response.StatusCode != HttpStatusCode.OK){
                String badResponseContent = await response.Content.ReadAsStringAsync();
                throw new Exception("Response Status Code: "+response.StatusCode+"\nResponse Message: "+ badResponseContent); 
            } 

            //send requests
            var responseContent = await response.Content.ReadAsStreamAsync();
            
            //get length of encypted sums array
            responseContent.Seek(-8, SeekOrigin.End);
            BinaryReader reader = new BinaryReader(responseContent);  
            UInt64 sizeOfEncryptedWeightedSums = reader.ReadUInt64();

            //get length of entire list of encrypted sums from last 8 bytes of stream (used to determine the end, in the memory streams, of the list of encrypted sums)  
            responseContent.Seek((long)sizeOfEncryptedWeightedSums, SeekOrigin.Begin);
            UInt64  lengthOfWeightedSumSizes =  (UInt64)responseContent.Length - (sizeOfEncryptedWeightedSums + 8);
            
            //get array of the sizes of each individual encrypted weighted sum 
            MemoryStream tempStream = new MemoryStream(reader.ReadBytes((int)lengthOfWeightedSumSizes));
            long[] weightSumSizes = (long[]) formatter.Deserialize(tempStream);
            tempStream.Close();

            //get list of encrypted sums
            responseContent.Seek(0, SeekOrigin.Begin);
            tempStream = new MemoryStream(reader.ReadBytes((int)sizeOfEncryptedWeightedSums));
            tempStream.Seek(0, SeekOrigin.Begin);
            
            //extract encrypted sum from stream
            List<List<Ciphertext>> encryptedWeightedSums = Logics.EncryptedMLHelper.extract2DCipherListFromStream(tempStream, weightSumSizes, contextManager.Context);
            tempStream.Close();

            // return URI of the created resource. 
            return encryptedWeightedSums;
        }
        static void Main()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {

            KeyPair keyPair = keyManager.CreateKeys();

            using PublicKey publicKey = keyPair.publicKey;
            using SecretKey secretKey = keyPair.secretKey;

            String FileName = "";
            
            String applicationName = "Machine Learning with Encryption"; 
            String version = "Prototype v 1.0.0";

            Console.Write(new String('-', Console.WindowWidth));
            Console.SetCursorPosition((Console.WindowWidth - applicationName.Length) / 2, Console.CursorTop);
            Console.WriteLine(applicationName);

            Console.SetCursorPosition((Console.WindowWidth - "______".Length) / 2, Console.CursorTop);
            Console.WriteLine("______");
            Console.SetCursorPosition((Console.WindowWidth - "/  __  \\".Length) / 2, Console.CursorTop);
            Console.WriteLine("/  __  \\");
            Console.SetCursorPosition((Console.WindowWidth - "_|_|__|_|_".Length) / 2, Console.CursorTop);
            Console.WriteLine("_|_|__|_|_");
            Console.SetCursorPosition((Console.WindowWidth - "|          |".Length) / 2, Console.CursorTop);
            Console.WriteLine("|          |");
            Console.SetCursorPosition((Console.WindowWidth - "|  M.L.E.  |".Length) / 2, Console.CursorTop);
            Console.WriteLine("|  M.L.E.  |");
            Console.SetCursorPosition((Console.WindowWidth - "|__________|".Length) / 2, Console.CursorTop);
            Console.WriteLine("|__________|");


            Console.Write(new String('-', Console.WindowWidth));
            Console.WriteLine(version+"\n");
            try
            {
                Console.Write("\tEnter the name of the CSV containing the samples to be encrypted: ");
                FileName = Console.ReadLine();

                string fileExtension = Path.GetExtension(FileName);
                if (fileExtension != ".csv")
                {
                    Console.WriteLine("file type must be CSV");
                    return;
                }

                string filePath = Path.GetFullPath(Directory.GetCurrentDirectory()) + "\\"+  FileName;
                if ( ! File.Exists(filePath)){
                    throw new Exception("File "+filePath+" does not exists.");
                }
                List<List<float>> featureList = Logics.EncryptedMLHelper.ReadValuesToList(filePath);
                
                List<List<Ciphertext>> encryptedFeatureValues = Logics.EncryptedMLHelper.encryptValues(featureList, publicKey, contextManager.Context);

                client.BaseAddress = new Uri("https://mle.isot.ca/");
                client.DefaultRequestHeaders.Accept.Clear();
            

                Query query = new Query
                {
                    publicKey = publicKey,
                    encryptedFeatureValues = encryptedFeatureValues

                };
               

                Console.WriteLine("\tSending Query...");
                List<List<Ciphertext>> encryptedWeightedSums = await getPredictions(query);
                
                var results = Logics.EncryptedMLHelper.decryptValues(encryptedWeightedSums, secretKey, contextManager.Context);
                
                var csv = new StringBuilder();

                using (System.IO.StreamWriter file = new System.IO.StreamWriter("Result.csv")){
                    foreach (var sample in results){
                        for (int i = 0; i < (sample.Count -1 ); i++ ){
                            file.Write(sample[i].ToString()+",");
                        }
                        file.Write(sample[sample.Count -1 ].ToString()+"\n");
                    }
                }
                Console.WriteLine("\n\tSuccess, Results saved to Results.csv.\n");
            
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\tError:");
                Console.WriteLine("\t"+e.Message);
                if (e.InnerException != null){
                    Console.WriteLine("\t\t"+e.InnerException.Message);
                }
                Console.WriteLine("\n");
            }
        }
    }
}