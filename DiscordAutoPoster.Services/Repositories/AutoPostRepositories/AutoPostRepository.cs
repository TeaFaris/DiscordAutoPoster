using Data;
using DiscordAutoPoster.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DiscordAutoPoster.Services.Repositories.AutoPostRepositories
{
    public class AutoPostRepository : IAutoPostRepository
    {
        readonly ApplicationDbContext dbContext;
        public AutoPostRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task AddAsync(AutoPost entity)
        {
            await dbContext.AutoPosts.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<AutoPost> entities)
        {
            await dbContext.AutoPosts.AddRangeAsync(entities);
        }

        public async Task<IEnumerable<AutoPost>> FindAsync(Expression<Func<AutoPost, bool>> predicate)
        {
            return await dbContext
                .AutoPosts
                .Include(x => x.Owner)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AutoPost>> GetAllAsync()
        {
            return await dbContext
                .AutoPosts
                .Include(x => x.Owner)
                .ToListAsync();
        }

        public async Task<AutoPost?> GetAsync(int id)
        {
            return await dbContext
                .AutoPosts
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task RemoveAsync(AutoPost entity)
        {
            dbContext.AutoPosts.Remove(entity);
            return Task.CompletedTask;
        }

        public Task RemoveRangeAsync(IEnumerable<AutoPost> entities)
        {
            dbContext.AutoPosts.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await dbContext.SaveChangesAsync();
        }

        public Task UpdateAsync(AutoPost entity)
        {
            dbContext.AutoPosts.Update(entity);
            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(IEnumerable<AutoPost> entities)
        {
            dbContext.AutoPosts.UpdateRange(entities);
            return Task.CompletedTask;
        }
    }
}
