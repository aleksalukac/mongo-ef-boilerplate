using MongoDB.Bson;
using System;

namespace WebApi.Models.Accounts
{
    public class AccountResponse
    {
        public ObjectId _id { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool IsVerified { get; set; }
    }
}