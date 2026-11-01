using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Enums;
using WindCalcEngine.Contracts.Interfaces;
using WindCalcEngine.Contracts.Models;
using WindCalcEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WindCalcEngine.Infrastructure.Repositories
{
    /// <summary>
    /// 关系型数据库仓储实现 (当前底层为 SQLite)
    /// </summary>
    public class RelationalRepository : IRelationalRepository
    {
        private readonly WindEngineDbContext _dbContext;

        public RelationalRepository(WindEngineDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 保存计算出的单机性能指标
        /// 解决 SQLite 并发问题：采用 Upsert (存在则更新，不存在则插入) 逻辑
        /// </summary>
        public async Task SaveTurbineEfficiencyResultAsync(TurbineEfficiencyResultDto result, CancellationToken ct = default)
        {
            var existing = await _dbContext.EfficiencyResults
                .FirstOrDefaultAsync(e => e.UnitId == result.UnitId && e.StatDate == result.StatDate, ct);

            if (existing == null)
            {
                await _dbContext.EfficiencyResults.AddAsync(result, ct);
            }
            else
            {
                // 更新当前记录的值
                _dbContext.Entry(existing).CurrentValues.SetValues(result);
            }

            // 统一提交事务，释放 SQLite 写锁
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpdateTaskStatusAsync(string taskId, TaskStateEnum state, string? errorMsg = null, CancellationToken ct = default)
        {
            var task = await _dbContext.TaskLogs.FindAsync(new object[] { taskId }, ct);
            if (task == null)
            {
                task = new TaskLogEntity { TaskId = taskId, State = state, ErrorMessage = errorMsg, UpdatedAt = DateTime.Now };
                await _dbContext.TaskLogs.AddAsync(task, ct);
            }
            else
            {
                task.State = state;
                task.ErrorMessage = errorMsg ?? task.ErrorMessage;
                task.UpdatedAt = DateTime.Now;
            }
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<RectificationRecord>> GetRectificationRecordsAsync(string unitId, CancellationToken ct = default)
        {
            return await _dbContext.RectificationRecords
                .Where(r => r.UnitId == unitId)
                .OrderByDescending(r => r.RectificationDate)
                .ToListAsync(ct);
        }

        public async Task SaveMlModelMetadataAsync(MlModelMetadata metadata, CancellationToken ct = default)
        {
            var existing = await _dbContext.MlModelMetadatas.FindAsync(new object[] { metadata.ModelId }, ct);
            if (existing == null)
            {
                await _dbContext.MlModelMetadatas.AddAsync(metadata, ct);
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(metadata);
            }
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<MlModelMetadata?> GetLatestMlModelMetadataAsync(string unitId, CancellationToken ct = default)
        {
            return await _dbContext.MlModelMetadatas
                .Where(m => m.UnitId == unitId)
                .OrderByDescending(m => m.TrainedAt)
                .FirstOrDefaultAsync(ct);
        }
    }
}