using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameTurnEntity
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("gameId")]
        public Guid GameId { get; set; }

        [BsonElement("turnNumber")]
        public int TurnNumber { get; set; }

        [BsonElement("firstPlayerMove")]
        public Move FirstPlayerMove { get; set; }

        [BsonElement("secondPlayerMove")]
        public Move SecondPlayerMove { get; set; }

        [BsonElement("result")]
        public TurnResult Result { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}