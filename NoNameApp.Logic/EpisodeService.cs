using NoNameApp.BLL.Infrastructure;
using NoNameApp.DAL.EF;
using NoNameApp.Entities;
using NoNameApp.LogicContracts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoNameApp.Logic
{
    public class EpisodeService : IEpisodeService
    {
        private DAL.EF.AppContext db;
        public EpisodeService (DAL.EF.AppContext context)
        {
            db = context;
        }



    public Episode GetEpisode(int? id)
        {
            if (id == null)
                throw new ValidationException("Не установлено id эпизода", "");
            var episode = db.Episodes.Find(id);
           
            if (episode == null)
                throw new ValidationException("сериал не эпизода", "");
            return episode;
           
        }
    }
}