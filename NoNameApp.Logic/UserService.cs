using NoNameApp.Entities;
using NoNameApp.BLL.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using NoNameApp.LogicContracts;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Data.Entity;
using NoNameApp.DAL.EF;
using NoNameApp.Logic;
using System;

namespace NoNameApp.Logic
{
    public class UserService : IUserService
    {
        
        private DAL.EF.AppContext db;
        public UserService(DAL.EF.AppContext context)
        {
            db = context;
        }

        public User GetUser(string email)
        {
            if (email == null)
                throw new ValidationException("Не установлено email юзера", "");
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                throw new ValidationException("Юзер не найден", "");
            return (user);
        }

        public List<IGrouping<Serial, IGrouping<Season, Episode>>> GetUserSerials(User user)
        {
            var userSerials = db.Serials
                .ToList()
                .Where(serial => user.Serials.Contains(serial));
            var ep = userSerials
                       .SelectMany(serial => serial.Seasons)
                       .SelectMany(season => season.Episodes)
                       .Where(episode => !user.Episodes.Contains(episode) && episode.Date < DateTime.Now)
                       .GroupBy(episode => episode.Season)
                       .GroupBy(group => group.Key.Serial)
                       .ToList();
            return ep;
        }

        public List<Episode> GetUserEpisode(User user)
        {
            var userSerials = db.Serials
                .ToList()
                .Where(serial => user.Serials.Contains(serial));
            var allEp = userSerials
                            .SelectMany(season => season.Seasons)
                            .SelectMany(Episode => Episode.Episodes)
                            .Where(episode => episode.Date > DateTime.Now)
                            .OrderBy(episode => episode.Date.DayOfYear).ToList();
            return allEp;
        }

        public void AddSerial(User user, Serial serial)
        {
            if (user == null)
                throw new ValidationException("Юзер не найден", "");
            user.Serials.Add(serial);
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void DelSerial(User user, Serial serial)
        {
            if (user == null)
                throw new ValidationException("Юзер не найден", "");
            user.Serials.Remove(serial);
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void DelAllEpisode(User user, Serial serial)
        {
            if (user == null || serial == null)
                throw new ValidationException("Юзер или эпизод не найден и не могут быть удалены", "");
            var episodes = serial.Seasons
                            .SelectMany(Episode => Episode.Episodes)
                            .ToList();
            foreach (var episode in episodes)
            {
                user.Episodes.Remove(episode);
            }
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void AddEpisode(User user, Episode episode)
        {
            user.Episodes.Add(episode);
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
        }

        public bool CheckSerial(User user, Serial serial)
        {
            if (db.Serials.ToList().Where(serials => user.Serials.Contains(serial)).Count() == 0)
                return false;
            else
                return true;
        }

        public Serial WatchedEp(User user, Serial serial)
        {
            if (user == null || serial == null)
                throw new ValidationException("Юзер или сериал не найдены", "");
            var episodes = serial.Seasons
                            .SelectMany(episode => episode.Episodes)
                            .ToList();
            var allUserEpisoded = user.Episodes
                                        .ToList();
            foreach (var episode in episodes)
                episode.Wacthed = false;
            for (int i = 0; i < episodes.Count(); i++)
            {
                for (int k = 0; k < allUserEpisoded.Count(); k++)
                {
                    if (episodes[i].Id == allUserEpisoded[k].Id)
                    {
                        episodes[i].Wacthed = true;
                    }
                }
            }
            return serial;
        }

        public void AddRating(User user, Serial serial, int star)
        {
            if (star > 5 || star < 0)
                throw new ValidationException("Оценка задана неверно", "");
            var userRate = user.Ratings.ToList();
            bool check = false;
            if (userRate.Count == 0)
                NewRate(user, serial, star);
            else
            {
                for (int i = 0; i < userRate.Count; i++)
                {
                    if (userRate[i].SerialId == serial.Id)
                    {
                        userRate[i].ValueRating = star;
                        check = true;
                    }
                }
                if (check == false) NewRate(user, serial, star);
            }
            db.SaveChanges();
        }

        public int GetRateUser(User user, Serial serial)
        {
            var userRate = user.Ratings.ToList();
            for (int i = 0; i < userRate.Count; i++)
            {
                if (userRate[i].SerialId == serial.Id)
                    return userRate[i].ValueRating;
            }
            return 0;
        }

        public int SpentTime(User user)
        {
            var userSerials = user.Serials.ToList();
            var spentTime = userSerials.
                Select(serial => new
                {
                    Ser = serial.Duration * user.Episodes.Where(s => s.Season.SerialId == serial.Id).Count()
                })
                .Sum(s => s.Ser);
            return spentTime;
        }

        public OperationDetails EditUser(User user, string OldPassword, string NewPassword)
        {
            if(OldPassword==null|| NewPassword == null)
                throw new ValidationException("Поля новый и старый пароль не должны быть пустыми", "");
            if (user.Password == OldPassword)
            {
                user.Password = NewPassword;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return new OperationDetails(true, "Пароль изменен", "");
            }
            else
                return new OperationDetails(false, "Неверный старый пароль", "");
        }

        public void Dispose()
        {
            db.Dispose();
        }
        

        public void Remove(int? id)
        {
            if (id == null)
                throw new ValidationException("Не установлено id юзера", "");
            var user = db.Users.Find(id.Value);
            if (user == null)
                throw new ValidationException("Юзер не найден", "");
            db.Users.Remove(user);
            db.SaveChanges();
        }

        public async Task<OperationDetails> Create(User user)
        {
            if (user == null)
                throw new ValidationException("Ошибка добавление юзера", "");
            if (user.Email == null || user.Password.Length < 6)
                throw new ValidationException("Логин или пароль пустые или не верные", "");
            User userSearch = await db.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (userSearch == null)
            {
                user = new User { RoleId = 2, Email = user.Email, Password = user.Password };
                db.Users.Add(user);
                db.SaveChanges();
                return new OperationDetails(true, "Регистрация успешно пройдена", "");
            }
            else
            {
                return new OperationDetails(false, "Пользователь с таким логином уже существует", "Email");
            }
        }

        public async Task<ClaimsIdentity> Authenticate(String email, String password)
        {
            ClaimsIdentity claim = null;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password==password);
            if (user==null)
               throw new ValidationException("Неверный логин или пароль", "");
            if (user != null)
            {
                claim = new ClaimsIdentity("ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                claim.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(), ClaimValueTypes.String));
                claim.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email, ClaimValueTypes.String));
                claim.AddClaim(new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider",
                    "OWIN Provider", ClaimValueTypes.String));
                if (user.Role != null)
                    claim.AddClaim(new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.Name, ClaimValueTypes.String));
            }
            return claim;
        }

        private void NewRate(User user, Serial serial, int star)
        {
            Rating rating = new Rating
            {
                SerialId = serial.Id,
                UserId = user.Id,
                ValueRating = star
            };
            db.Ratings.Add(rating);
        }
        
    }
}
