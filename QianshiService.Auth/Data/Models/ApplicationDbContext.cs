using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace QianshiService.Auth.Data.Models
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<UserInfo> UserInfos { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt) : base(opt)
        {
        }
    }
}
