using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Enums
{
    /// <summary>
    /// 数据清洗质量标记
    /// </summary>
    public enum DataQualityEnum
    {
        Good = 1,           // 原始数据完好
        Interpolated = 2,   // 缺失插值 (如使用临近机组风速填补)
        Clamped = 3,        // 越界修正 (如将 600hPa 异常气压强制拉回 1012hPa)
        Bad = 4             // 坏死数据，丢弃不参与积分
    }
}
