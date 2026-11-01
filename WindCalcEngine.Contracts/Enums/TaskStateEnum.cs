using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Enums
{
    /// <summary>
    /// 计算任务运行状态
    /// </summary>
    public enum TaskStateEnum
    {
        Pending = 0,
        CleaningData = 1,   // 数据清洗中
        Calculating = 2,    // 物理模型计算中
        TrainingModel = 3,  // 新增：AI模型训练中
        Completed = 4,
        Failed = 5
    }
}
