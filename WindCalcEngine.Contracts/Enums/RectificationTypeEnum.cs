using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Enums
{
    /// <summary>
    /// 机组技改/优化类型 (新增：用于整改评价)
    /// </summary>
    public enum RectificationTypeEnum
    {
        YawCalibration = 1,       // 偏航风向标校准
        PitchSystemUpgrade = 2,   // 变桨系统/轴承升级
        ControlLogicFlash = 3,    // 主控程序(PLC)刷机优化
        BladeCleaning = 4,        // 叶片清洗/气动修复
        Other = 99
    }
}
