using System;
using System.Threading.Tasks;

namespace MarginTrading.Common.Settings.Repositories
{
    public interface IClientAccount
    {
        DateTime Registered { get; }
        string Id { get; }
        string Email { get; }
        string Phone { get; }
        string Pin { get; }
        string NotificationsId { get; }
    }

    public class ClientAccount : IClientAccount
    {
        public DateTime Registered { get; set; }
        public string Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Pin { get; set; }
        public string NotificationsId { get; set; }

        public static ClientAccount Create(string email, string phone)
        {
            return new ClientAccount
            {
                Email = email,
                Registered = DateTime.UtcNow,
                Phone = phone
            };
        }
    }

    public interface IClientAccountsRepository
    {
        Task<IClientAccount> GetByIdAsync(string id);
        Task<IClientAccount> GetByEmailAsync(string email);
    }
}
