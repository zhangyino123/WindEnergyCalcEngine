using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Contracts.Interfaces
{
    /// <summary>
    /// 分布式缓存服务契约 (对应 Redis)
    /// 高频访问的静态档案必须走 Redis，防止拖垮 PostgreSQL
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取风机设计参数 (包含 NTF 修正系数、额定参数)
        /// </summary>
        Task<TurbineStaticConfig> GetTurbineConfigAsync(string unitId);

        /// <summary>
        /// 获取全场装机总容量 (MW) - 用于 AGC 限电阈值判定
        /// </summary>
        Task<double> GetFarmCapacityMwAsync(string farmId);
    }
}
