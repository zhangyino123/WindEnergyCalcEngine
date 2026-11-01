using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.DTOs
{
    /// <summary>
    /// 供前端 ECharts 渲染的曲线点位
    /// </summary>
    public record PowerCurvePointDto(double WindSpeed, double Power);
}
