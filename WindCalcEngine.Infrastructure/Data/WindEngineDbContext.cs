using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Enums;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Infrastructure.Data
{
    /// <summary>
    /// 风电能效关系型业务数据库上下文 (支持 SQLite / PG / 达梦)
    /// 严禁在此数据库中存储秒级或10分钟级的 SCADA 测点流水！
    /// </summary>
    public class WindEngineDbContext : DbContext
    {
        public WindEngineDbContext(DbContextOptions<WindEngineDbContext> options) : base(options)
        {
            // 核心：如果数据库文件不存在，自动创建完整的 .db 文件和表结构！
            Database.EnsureCreated();
        }

        // 1. 机组静态档案表 (用于替代 Redis 的本地直读)
        public DbSet<TurbineStaticConfig> TurbineConfigs { get; set; }

        // 2. 每日/月度能效指标结果大表 (包含所有PBA、损失MWh、异常点数)
        public DbSet<TurbineEfficiencyResultDto> EfficiencyResults { get; set; }

        // 3. 技改整改历史台账表 (A/B Test 依据)
        public DbSet<RectificationRecord> RectificationRecords { get; set; }

        // 4. 机器学习自适应基线模型元数据表
        public DbSet<MlModelMetadata> MlModelMetadatas { get; set; }

        // 5. 任务调度执行状态日志表
        public DbSet<TaskLogEntity> TaskLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= 1. 机组静态档案表 =================
            modelBuilder.Entity<TurbineStaticConfig>(entity =>
            {
                entity.ToTable("biz_turbine_config");
                entity.HasKey(e => e.UnitId);
                // EF Core 在 SQLite 中存储数组需转换为 JSON 字符串
                entity.Property(e => e.NtfCoefficients)
                      .HasConversion(
                          v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                          v => System.Text.Json.JsonSerializer.Deserialize<double[]>(v, (System.Text.Json.JsonSerializerOptions)null!)!
                      );
                // 忽略只读属性
                entity.Ignore(e => e.SweptArea);
            });

            // ================= 2. 核心 KPI 指标表 =================
            modelBuilder.Entity<TurbineEfficiencyResultDto>(entity =>
            {
                entity.ToTable("kpi_turbine_efficiency_result");
                // 联合主键：机组ID + 统计日期
                entity.HasKey(e => new { e.UnitId, e.StatDate });
            });

            // ================= 3. 技改整改记录表 =================
            modelBuilder.Entity<RectificationRecord>(entity =>
            {
                entity.ToTable("biz_rectification_record");
                entity.HasKey(e => e.RecordId);
            });

            // ================= 4. ML.NET 模型元数据表 =================
            modelBuilder.Entity<MlModelMetadata>(entity =>
            {
                entity.ToTable("biz_ml_model_metadata");
                entity.HasKey(e => e.ModelId);
            });

            // ================= 5. 系统任务日志表 =================
            modelBuilder.Entity<TaskLogEntity>(entity =>
            {
                entity.ToTable("sys_task_log");
                entity.HasKey(e => e.TaskId);
            });

        }

    }
}
