using System;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {
            userCollection = database.GetCollection<UserEntity>(CollectionName);
            var loginIndex = new CreateIndexModel<UserEntity>(
                Builders<UserEntity>.IndexKeys.Ascending(user => user.Login),
                new CreateIndexOptions { Unique = true });
            userCollection.Indexes.CreateOne(loginIndex);
        }

        public UserEntity Insert(UserEntity user)
        {
            if (user.Id != Guid.Empty)
                throw new InvalidOperationException();

            var entity = new UserEntity(Guid.NewGuid(), user.Login, user.LastName,
                user.FirstName, user.GamesPlayed, user.CurrentGameId);
            userCollection.InsertOne(entity);
            return entity;
        }

        public UserEntity FindById(Guid id)
        {
            return userCollection.Find(user => user.Id == id).FirstOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            var filter = Builders<UserEntity>.Filter.Eq(user => user.Login, login);
            var update = Builders<UserEntity>.Update
                .SetOnInsert(user => user.Id, Guid.NewGuid())
                .SetOnInsert(user => user.Login, login)
                .SetOnInsert(user => user.LastName, null)
                .SetOnInsert(user => user.FirstName, null)
                .SetOnInsert(user => user.GamesPlayed, 0)
                .SetOnInsert(user => user.CurrentGameId, null);
            var options = new FindOneAndUpdateOptions<UserEntity>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            try
            {
                return userCollection.FindOneAndUpdate(filter, update, options);
            }
            catch (MongoCommandException exception) when (exception.Code == 11000)
            {
                // Параллельный upsert мог первым создать пользователя с этим логином.
                var concurrentUser = userCollection.Find(filter).FirstOrDefault();
                if (concurrentUser != null)
                    return concurrentUser;
                throw;
            }
        }

        public void Update(UserEntity user)
        {
            userCollection.ReplaceOne(entity => entity.Id == user.Id, user);
        }

        public void Delete(Guid id)
        {
            userCollection.DeleteOne(user => user.Id == id);
        }

        // Для вывода списка всех пользователей (упорядоченных по логину)
        // страницы нумеруются с единицы
        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            var filter = Builders<UserEntity>.Filter.Empty;
            var totalCount = userCollection.CountDocuments(filter);
            var items = userCollection.Find(filter)
                .SortBy(user => user.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToList();
            return new PageList<UserEntity>(items, totalCount, pageNumber, pageSize);
        }

        // Не нужно реализовывать этот метод
        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            throw new NotImplementedException();
        }
    }
}
