using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Entities
{
    public class BaseModel : IHasId
    {
        public ObjectId _id { get; set; }

        public ObjectId GetId()
        {
            return _id;
        }
    }

    public interface IHasId
    {
        public ObjectId GetId();
    }
}
