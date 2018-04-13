using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Migrations
{
    public abstract class AbstractMigration
    {
        public AbstractMigration()
        {
            
        }

        public virtual Task Invoke()
        {
            return Task.CompletedTask;
        }
    }
}