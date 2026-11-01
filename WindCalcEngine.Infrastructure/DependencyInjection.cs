using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Interfaces;
using WindCalcEngine.Infrastructure.Data;
using WindCalcEngine.Infrastructure.Repositories;
using WindCalcEngine.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Minio;

namespace WindCalcEngine.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // =========================================================================
            // 1. 关系型数据库注入 (SQLite -> PG/达梦 无缝切换处)
            // =========================================================================
            services.AddDbContext<WindEngineDbContext>(options =>
            {
                // 【当前：测试/单机部署模式，使用 SQLite】
                options.UseSqlite(configuration.GetConnectionString("DefaultSqliteConnection"));

                // 【未来：生产交付模式，注释掉上一行，取消下一行的注释即可接入 PG / 达梦】
                // options.UseNpgsql(configuration.GetConnectionString("PgSqlConnection"));
                // options.UseDm(configuration.GetConnectionString("DmConnection")); // 人大金仓或达梦
            });

            services.AddScoped<IRelationalRepository, RelationalRepository>();

            // =========================================================================
            // 2. 时序数据库注入
            // =========================================================================
            string tsdbConn = configuration.GetConnectionString("TDengineConnection") ?? "";
            services.AddScoped<ITimeSeriesRepository>(provider => new TimeSeriesRepository(tsdbConn));

            // =========================================================================
            // 3. Redis 缓存注入
            // =========================================================================
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisConnection");
                options.InstanceName = "WindCalc_";
            });
            services.AddScoped<ICacheService, RedisCacheService>();

            // =========================================================================
            // 4. MinIO 对象存储注入
            // =========================================================================
            var minioEndpoint = configuration["MinIO:Endpoint"];
            var minioAccessKey = configuration["MinIO:AccessKey"];
            var minioSecretKey = configuration["MinIO:SecretKey"];

            services.AddMinio(configureClient => configureClient
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .WithSSL(false)
                .Build());

            services.AddScoped<IObjectStorageService, MinioStorageService>();

            return services;
        }
    }
}
