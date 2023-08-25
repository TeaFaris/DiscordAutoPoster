using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Linq.Expressions;

namespace DiscordAutoPoster.Services.Repositories.UserRepositories
{
    public class UserRepository : IUserRepository
    {
        readonly ApplicationDbContext dbContext;
        public UserRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task AddAsync(ApplicationUser entity)
        {
            await dbContext.Users.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<ApplicationUser> entities)
        {
            await dbContext.Users.AddRangeAsync(entities);
        }

        public async Task<IEnumerable<ApplicationUser>> FindAsync(Expression<Func<ApplicationUser, bool>> predicate)
        {
            return await dbContext
                .Users
                .Include(x => x.CurrentAutoPost)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await dbContext
                .Users
                .Include(x => x.CurrentAutoPost)
                .ToListAsync();
        }

        public async Task<ApplicationUser?> GetAsync(int id)
        {
            return await dbContext
                .Users
                .Include(x => x.CurrentAutoPost)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task RemoveAsync(ApplicationUser entity)
        {
            dbContext.Users.Remove(entity);
            return Task.CompletedTask;
        }

        public Task RemoveRangeAsync(IEnumerable<ApplicationUser> entities)
        {
            dbContext.Users.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await dbContext.SaveChangesAsync();
        }

        public Task UpdateAsync(ApplicationUser entity)
        {
            dbContext.Users.Update(entity);
            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(IEnumerable<ApplicationUser> entities)
        {
            dbContext.Users.UpdateRange(entities);
            return Task.CompletedTask;
        }
    }
}
