using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CDTS_PROJECT.Models
{
    public class Model
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Type")]
        public string Type { get; set; }

        [BsonElement("Precision")]
        public int Precision { get; set; }

        [BsonElement("MClasses")]
        public int M_classes { get; set; }

        [BsonElement("NWeights")]
        public int N_weights { get; set; }

        [BsonElement("Weights")]
        public List<double[]> Weights { get ; set; }

        public string toString(){
            return "Type: "+Type+"\tNo. of Classes "+M_classes+"\tNo. of features "+N_weights; 
        }
    }

    
}