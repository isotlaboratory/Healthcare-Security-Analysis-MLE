using CDTS_PROJECT.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace CDTS_PROJECT.Services
{

    public interface IModelService
    {
        List<Model> Get(); 

        Model Get(string Type);

        Model Create(Model model);

        void Update(string id, Model modelIn);

        void Remove(Model modelIn);

        void Remove(string id);

    }

    public class ModelService :  IModelService
    {
        private readonly IMongoCollection<Model> _model;

        public ModelService(IModelDatabaseSettings settings)
        {
            MongoClient client = new MongoClient(settings.ConnectionString);
            IMongoDatabase database = client.GetDatabase(settings.DatabaseName);

            _model = database.GetCollection<Model>(settings.ModelsCollectionName);
        }

        public List<Model> Get() =>
            _model.Find(model => true).ToList();

        public Model Get(string Type) =>
            _model.Find<Model>(model => model.Type == Type).FirstOrDefault();

        public Model Create(Model model)
        {
            _model.InsertOne(model);
            return model;
        }

        public void Update(string Type, Model modelIn) =>
            _model.ReplaceOne(model => model.Type == Type, modelIn);

        public void Remove(Model modelIn) =>
            _model.DeleteOne(model => model.Type == modelIn.Type);

        public void Remove(string Type) => 
            _model.DeleteOne(model => model.Type == Type);
    }
}