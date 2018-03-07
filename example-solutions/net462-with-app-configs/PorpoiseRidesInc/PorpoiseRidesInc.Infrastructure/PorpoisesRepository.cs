using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;
using System.Data.Entity;

namespace PorpoiseRidesInc.Infrastructure
{
    public class PorpoisesRepository : IPorpoiseRepository
    {
        public async Task AddAsync(Porpoise porpoise)
        {
            using (var context = new PorpoiseRidesContext())
            {
                context.Porpoises.Add(porpoise);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Porpoise>> GetAsync()
        {
            using (var context = new PorpoiseRidesContext())
            {
                return await context.Porpoises.ToListAsync();
            }
        }

        public async Task<int> GetCountAsync(string species)
        {
            using (var context = new PorpoiseRidesContext())
            {
                return await context.Porpoises.CountAsync(x => x.Species.Equals(species));
            }
        }

        public async Task UpdateAsync(Porpoise porpoise)
        {
            using (var context = new PorpoiseRidesContext())
            {
                var porpoiseDb = context.Porpoises.FirstOrDefault(x => x.Id == porpoise.Id);
                porpoiseDb.Name = porpoise.Name;
                porpoiseDb.Species = porpoise.Species;
                await context.SaveChangesAsync();
            }
        }
    }
}
