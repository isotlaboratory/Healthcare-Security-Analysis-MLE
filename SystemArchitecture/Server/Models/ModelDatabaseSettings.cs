namespace CDTS_PROJECT.Models
{
    public class ModelDatabaseSettings : IModelDatabaseSettings
    {
        public string ModelsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IModelDatabaseSettings
    {
        string ModelsCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}