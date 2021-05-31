using System.Threading.Tasks;

namespace ImportXC.Services.Importers
{
    public interface IImporter
    {
        public Task Import();
    }
}