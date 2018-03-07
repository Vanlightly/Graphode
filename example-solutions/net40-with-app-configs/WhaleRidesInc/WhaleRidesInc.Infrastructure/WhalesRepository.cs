using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;
using WhaleRidesInc.BusinessLogic.InfrastructureContracts;

namespace WhaleRidesInc.Infrastructure
{
    public class WhalesRepository : IWhaleRepository
    {
        public void Add(Whale whale)
        {
            using (var context = new WhaleRides())
            {
                context.Whales.Add(whale);
                context.SaveChanges();
            }
        }

        public List<Whale> Get()
        {
            using (var context = new WhaleRides())
            {
                return context.Whales.ToList();
            }
        }

        public int GetCount(string species)
        {
            using (var context = new WhaleRides())
            {
                return context.Whales.Count(x => x.Species.Equals(species));
            }
        }

        public void Update(Whale whale)
        {
            using (var context = new WhaleRides())
            {
                var whaleDb = context.Whales.FirstOrDefault(x => x.Id == whale.Id);
                whaleDb.Name = whale.Name;
                whaleDb.Species = whale.Species;
                context.SaveChanges();
            }
        }
    }
}
