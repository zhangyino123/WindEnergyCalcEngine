using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using WindCalcEngine.Contracts.Interfaces;
using WindCalcEngine.Contracts.Models;
using Dapper;

namespace WindCalcEngine.Infrastructure.Repositories
{
    /// <summary>
    /// 时序数据库访问实现 (实战中注入 TDengine 或 麦杰 的 Connection)
    /// </summary>
    public class TimeSeriesRepository : ITimeSeriesRepository
    {
        private readonly string _tsdbConnectionString;

        public TimeSeriesRepository(string tsdbConnectionString)
        {
            _tsdbConnectionString = tsdbConnectionString;
        }

        private IDbConnection CreateConnection()
        {
            // TODO: 交付生产时，将此处替换为 new TaosConnection(_tsdbConnectionString) (TDengine)
            return new SqliteConnection(_tsdbConnectionString);
        }

        public async Task<IEnumerable<AggregatedScadaPoint>> GetAggregatedPointsAsync(string unitId, DateTime startTime, DateTime endTime, CancellationToken ct = default)
        {
            using var conn = CreateConnection();

            // 典型的时序库查询 SQL 模板
            string sql = @"
            SELECT 
                unit_id AS UnitId, 
                ts AS Timestamp, 
                active_power AS ActivePower, 
                wind_speed AS WindSpeed, 
                wind_direction AS WindDirection, 
                nacelle_position AS NacellePosition,
                pitch_angle1 AS PitchAngle1,
                pitch_angle2 AS PitchAngle2,
                pitch_angle3 AS PitchAngle3,
                generator_speed AS GeneratorSpeed,
                temperature AS Temperature,
                pressure AS Pressure,
                yaw_err_var AS YawErrorVariance
            FROM scada_10min 
            WHERE unit_id = @UnitId AND ts >= @StartTime AND ts <= @EndTime
            ORDER BY ts ASC";

            return await conn.QueryAsync<AggregatedScadaPoint>(sql, new { UnitId = unitId, StartTime = startTime, EndTime = endTime });
        }

        public async Task BulkInsertAggregatedPointsAsync(IEnumerable<AggregatedScadaPoint> points, CancellationToken ct = default)
        {
            // 时序库极度依赖批量写入 (Batch Insert)。
            // 比如 TDengine 的 STMT 绑定或 influxDB 的 Line Protocol。此处仅作契约实现。
            throw new NotImplementedException("请根据所选的具体时序库（TDengine/麦杰）引用对应的 SDK 实现批量写入。");
        }

        public async Task<IEnumerable<AgcSetpoint>> GetFarmAgcSetpointsAsync(string farmId, DateTime startTime, DateTime endTime, CancellationToken ct = default)
        {
            using var conn = CreateConnection();
            string sql = @"
            SELECT 
                farm_id AS FarmId, 
                ts AS Timestamp, 
                agc_limit_mw AS ActivePowerSetPointMw 
            FROM agc_log 
            WHERE farm_id = @FarmId AND ts >= @StartTime AND ts <= @EndTime
            ORDER BY ts ASC";

            return await conn.QueryAsync<AgcSetpoint>(sql, new { FarmId = farmId, StartTime = startTime, EndTime = endTime });
        }
    }
}
