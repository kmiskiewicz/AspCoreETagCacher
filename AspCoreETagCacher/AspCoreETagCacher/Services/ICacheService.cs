namespace AspCoreETagCacher.Services

{
    public interface ICacheService
    {
        void Store(string key, object content);

        void Store(string key, object content, int duration);

        T Get<T>(string key) where T : class;

        void Remove(string key);
    }
    public interface ICacheRedisService : ICacheService
    {

    }
    public interface ICacheMemoryService : ICacheService
    {

    }
}
