using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using WindCalcEngine.Contracts.Interfaces;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Infrastructure.Services
{
    /// <summary>
    /// 缓存服务实现 (基于 Redis / IDistributedCache)
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IRelationalRepository _dbRepo; // 用于穿透回源

        public RedisCacheService(IDistributedCache cache, IRelationalRepository dbRepo)
        {
            _cache = cache;
            _dbRepo = dbRepo;
        }

        public async Task<TurbineStaticConfig> GetTurbineConfigAsync(string unitId)
        {
            string cacheKey = $"turbine_cfg_{unitId}";
            string? cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<TurbineStaticConfig>(cachedData)!;
            }

            // TODO: 缓存穿透 - 如果 Redis 没有，则通过 EF Core 从 PostgreSQL 中加载并写入 Redis
            // 这里提供默认假数据防止崩溃，实际接入时查询数据库
            var config = new TurbineStaticConfig
            {
                UnitId = unitId,
                FarmId = "FARM_01",
                RatedPower = 5000,
                RotorDiameter = 200,
                DesignCp = 0.45,
                CutInWindSpeed = 2.5,
                CutOutWindSpeed = 25.0,
                WeibullScaleA = 7.0,
                WeibullShapeK = 2.0
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(config),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) });

            return config;
        }

        public async Task<double> GetFarmCapacityMwAsync(string farmId)
        {
            // 获取全场总容量 (逻辑同上)
            return await Task.FromResult(233.0);
        }
    }
}
