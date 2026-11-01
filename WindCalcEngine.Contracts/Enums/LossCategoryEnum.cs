using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Enums
{
    /// <summary>
    /// 散点多维归因分类枚举 (严格对应前端七色图谱与损失电量切分)
    /// </summary>
    public enum LossCategoryEnum
    {
        /// <summary>
        /// 正常发电 (0) - 贴合物理基线或处于合理风速畸变范围内
        /// </summary>
        Normal = 0,

        /// <summary>
        /// AGC限电受阻 (1) - 调度指令限制导致，免除性能考核
        /// </summary>
        AgcCurtailed = 1,

        /// <summary>
        /// 风速仪异常/冰冻 (2) - 功率远超理论极限，风杯冻滞或机械损坏
        /// </summary>
        AnemometerIcing = 2,

        /// <summary>
        /// 偏航失准 (3) - 机舱位置与实际风向夹角超过死区（如 >15°），导致捕风面积减小
        /// </summary>
        YawMisalignment = 3,

        /// <summary>
        /// 变桨异常 (4) - 未达额定满发区，桨叶提前收桨(>3°)或三叶片严重不同步
        /// </summary>
        PitchError = 4,

        /// <summary>
        /// 控制死区/扭矩塌陷 (5) - 排除以上机械/环境原因，有功功率依然断崖式跌破理论值的50%
        /// </summary>
        ControlDeadband = 5,

        /// <summary>
        /// 未知劣化 (6) - 轻度折损，未达严重塌陷阈值
        /// </summary>
        UnknownDegradation = 6
    }

 

   
}
