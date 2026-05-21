using Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Core.Services;

public class DataStorageService
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public DataStorageService(string dbPath = "IndustrialData.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase().Wait();
    }

    private async Task InitializeDatabase()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                CREATE TABLE IF NOT EXISTS DataRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceId TEXT NOT NULL,
                    DeviceName TEXT NOT NULL,
                    DataPointId TEXT NOT NULL,
                    DataPointName TEXT NOT NULL,
                    RawValue REAL,
                    ScaledValue REAL,
                    Unit TEXT,
                    IsValid INTEGER NOT NULL DEFAULT 1,
                    RecordTime TEXT NOT NULL,
                    IsUploaded INTEGER NOT NULL DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_device_id ON DataRecords(DeviceId);
                CREATE INDEX IF NOT EXISTS idx_record_time ON DataRecords(RecordTime);
                CREATE INDEX IF NOT EXISTS idx_is_uploaded ON DataRecords(IsUploaded);
            ";
            
            await conn.ExecuteAsync(sql);
            Log.Information("Database initialized successfully");
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task InsertRecordAsync(DataRecord record)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                INSERT INTO DataRecords (DeviceId, DeviceName, DataPointId, DataPointName, 
                    RawValue, ScaledValue, Unit, IsValid, RecordTime, IsUploaded)
                VALUES (@DeviceId, @DeviceName, @DataPointId, @DataPointName,
                    @RawValue, @ScaledValue, @Unit, @IsValid, @RecordTime, @IsUploaded)
            ";
            
            await conn.ExecuteAsync(sql, new
            {
                record.DeviceId,
                record.DeviceName,
                record.DataPointId,
                record.DataPointName,
                record.RawValue,
                record.ScaledValue,
                record.Unit,
                IsValid = record.IsValid ? 1 : 0,
                RecordTime = record.RecordTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                IsUploaded = record.IsUploaded ? 1 : 0
            });
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task InsertRecordsAsync(IEnumerable<DataRecord> records)
    {
        foreach (var record in records)
        {
            await InsertRecordAsync(record);
        }
    }

    public async Task<List<DataRecord>> GetRecordsByDeviceIdAsync(string deviceId, int limit = 1000)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                SELECT * FROM DataRecords 
                WHERE DeviceId = @DeviceId 
                ORDER BY RecordTime DESC 
                LIMIT @Limit
            ";
            
            var records = (await conn.QueryAsync<DataRecordDto>(sql, new { DeviceId = deviceId, Limit = limit }))
                .Select(FromDto)
                .ToList();
            
            return records;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<List<DataRecord>> GetRecordsByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                SELECT * FROM DataRecords 
                WHERE RecordTime BETWEEN @StartTime AND @EndTime 
                ORDER BY RecordTime DESC
            ";
            
            var records = (await conn.QueryAsync<DataRecordDto>(sql, new {
                StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
            }))
                .Select(FromDto)
                .ToList();
            
            return records;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<List<DataRecord>> GetUnuploadedRecordsAsync(int limit = 1000)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                SELECT * FROM DataRecords 
                WHERE IsUploaded = 0 
                ORDER BY RecordTime ASC 
                LIMIT @Limit
            ";
            
            var records = (await conn.QueryAsync<DataRecordDto>(sql, new { Limit = limit }))
                .Select(FromDto)
                .ToList();
            
            return records;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task MarkAsUploadedAsync(IEnumerable<long> recordIds)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = "UPDATE DataRecords SET IsUploaded = 1 WHERE Id IN @Ids";
            await conn.ExecuteAsync(sql, new { Ids = recordIds });
            
            Log.Information("Marked {Count} records as uploaded", recordIds.Count());
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task DeleteOldRecordsAsync(DateTime olderThan)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = "DELETE FROM DataRecords WHERE RecordTime < @OlderThan";
            var deleted = await conn.ExecuteAsync(sql, new { 
                OlderThan = olderThan.ToString("yyyy-MM-dd HH:mm:ss.fff") 
            });
            
            Log.Information("Deleted {Count} old records", deleted);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private static DataRecord FromDto(DataRecordDto dto)
    {
        return new DataRecord
        {
            Id = dto.Id,
            DeviceId = dto.DeviceId,
            DeviceName = dto.DeviceName,
            DataPointId = dto.DataPointId,
            DataPointName = dto.DataPointName,
            RawValue = dto.RawValue,
            ScaledValue = dto.ScaledValue,
            Unit = dto.Unit,
            IsValid = dto.IsValid != 0,
            RecordTime = DateTime.ParseExact(dto.RecordTime, "yyyy-MM-dd HH:mm:ss.fff", null),
            IsUploaded = dto.IsUploaded != 0
        };
    }

    private class DataRecordDto
    {
        public long Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DataPointId { get; set; } = string.Empty;
        public string DataPointName { get; set; } = string.Empty;
        public double? RawValue { get; set; }
        public double? ScaledValue { get; set; }
        public string? Unit { get; set; }
        public int IsValid { get; set; }
        public string RecordTime { get; set; } = string.Empty;
        public int IsUploaded { get; set; }
    }
}
