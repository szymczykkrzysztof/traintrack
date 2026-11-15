using Microsoft.EntityFrameworkCore;

namespace TrainTrack.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
