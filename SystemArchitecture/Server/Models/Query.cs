using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Models
{
    public class Query{
        //public PublicKey publicKey { get; set;}
        public List<List<Ciphertext>> encryptedFeatureValues { get; set;}
    }
}