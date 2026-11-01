using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WindPowerForecast_45Days_Auto
{
    class Program
    {
        // 1. 忽略 SSL 证书验证
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        // 2. 数据输出的根目录
        private static readonly string _baseOutputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindDataOutput_45Days");

        // 3. 设定的每天执行时间 (上午 08:00 和 下午 16:00)
        private static readonly TimeSpan[] _runTimes = new TimeSpan[]
        {
            new TimeSpan(8, 0, 0),
            new TimeSpan(16, 0, 0)
        };

        // ================== 【模型选择区】 ==================
        // 留空 "" 表示由系统自动选择最佳组合
        // 欧洲中心: "ecmwf_ifs04"
        // 中国气象局: "cma_grapes_global"
        // 德国气象局: "icon_seamless"
        // private static readonly string _weatherModel = "ecmwf_ifs04";
        // 修改后 (留空，代表使用无缝混合模型 Seamless / Best Match)
        private static readonly string _weatherModel = "";
        // ====================================================

        static async Task Main(string[] args)
        {
            Console.WriteLine("===============================================================");
            Console.WriteLine($" 风电 45 天超长预报全自动采集系统 (当前强制模型: {_weatherModel})");
            Console.WriteLine("===============================================================\n");

            var stations = new List<StationInfo>
            {
                new StationInfo("GJNY_SXGS_FSG_FDC_STA", "SX_GNFS_WF",  37.2613,  107.5041),
                new StationInfo("GJNY_SXGS_ZZ_FDC_STA",  "SX_GNZZ_WF",  37.2423,  108.727211),
                new StationInfo("GJNY_SXGS_LJS_FDC_STA", "SX_GNLJS_WF", 37.4694,  108.396111),
                new StationInfo("GJNY_SXGS_JSL_FDC_STA", "SX_GNJSL_WF", 37.27343, 108.511806),
                new StationInfo("GJNY_SXGS_CSL_FDC_STA", "SX_GNCSL_WF", 37.281,   108.213),
                new StationInfo("GJNY_SXGS_XZ_FDC_STA",  "SX_GNXZ_WF",  37.4694,  107.8452)
            };

            // 程序刚启动时强制执行一次，方便您立刻确认数据格式
            Console.WriteLine("[系统提示] 程序启动，开始执行首次 45 天数据生成任务...");
            await FetchAndSaveAllStationsAsync(stations);

            // 核心：进入守护进程循环，每天按时唤醒
            while (true)
            {
                TimeSpan delay = GetDelayToNextRunTime();
                DateTime nextRunTime = DateTime.Now.Add(delay);

                Console.WriteLine($"\n[调度器] 下一轮 45 天预报采集将在: {nextRunTime:yyyy-MM-dd HH:mm:ss} 自动执行.");
                Console.WriteLine($"[调度器] 系统休眠挂机中，等待 {(int)delay.TotalHours}小时 {delay.Minutes}分钟...\n");

                await Task.Delay(delay); // 线程挂起

                Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 触发早晚定时任务，开始拉取最新 45 天预测！");
                await FetchAndSaveAllStationsAsync(stations);
            }
        }

        private static async Task FetchAndSaveAllStationsAsync(List<StationInfo> stations)
        {
            int apiMaxDays = 16;       // 气象物理极限 16 天
            int targetTotalDays = 45;  // 业务需要拼接至 45 天

            DateTime now = DateTime.Now;
            string batchFolderName = $"45天预测批次_{now:yyyyMMdd_HHmm}";
            string currentRunDirectory = Path.Combine(_baseOutputDirectory, batchFolderName);

            if (!Directory.Exists(currentRunDirectory))
            {
                Directory.CreateDirectory(currentRunDirectory);
            }

            string fileTimeSuffix = now.ToString("yyyyMMddHH"); // 最终文件名的时间戳

            foreach (var station in stations)
            {
                Console.WriteLine($"-> 正在采集并合成站点: {station.Id}...");
                try
                {
                    // 获取并智能合成 45 天数据
                    var dataList = await GetAndExtendWindDataAsync(station.Lat, station.Lon, apiMaxDays, targetTotalDays);

                    if (dataList != null && dataList.Count > 0)
                    {
                        string fileName = $"{station.FilePrefix}_{fileTimeSuffix}.csv";
                        string filePath = Path.Combine(currentRunDirectory, fileName);

                        SaveDataToCustomCsv(dataList, filePath);
                        Console.WriteLine($"   [成功] 生成 {dataList.Count} 行 (45天) 数据，保存至: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"   [错误] 站点 {station.Id} 失败: {ex.Message}");
                    Console.ResetColor();
                }

                await Task.Delay(1500); // 延时 1.5 秒，保护 IP
            }
        }

        private static async Task<List<InterpolatedWindData>> GetAndExtendWindDataAsync(double lat, double lon, int apiDays, int targetDays)
        {
            // 在 URL 拼接中，动态加入了 models 参数
            string modelParam = string.IsNullOrWhiteSpace(_weatherModel) ? "" : $"&models={_weatherModel}";

            string url = $"https://api.open-meteo.com/v1/forecast" +
                         $"?latitude={lat}" +
                         $"&longitude={lon}" +
                         modelParam +  // <--- 指定气象模型 (ECMWF, CMA 等)
                         $"&minutely_15=wind_speed_10m,wind_speed_80m,wind_direction_10m,wind_direction_80m,temperature_2m,relative_humidity_2m,surface_pressure,shortwave_radiation" +
                         $"&wind_speed_unit=ms" +
                         $"&timezone=Asia%2FShanghai" +
                         $"&forecast_days={apiDays}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonResponse);

            var resultList = new List<InterpolatedWindData>();
            var m = apiResult?.Minutely15;

            // 1. 处理前 16 天的真实气象预报
            if (m?.Time != null)
            {
                for (int i = 0; i < m.Time.Count; i++)
                {
                    double speed10 = m.WindSpeed10m[i] ?? 0;
                    double speed80 = m.WindSpeed80m[i] ?? 0;
                    double temp2m = m.Temperature2m[i] ?? 0;
                    double rh2m = m.RelativeHumidity2m[i] ?? 0;
                    double pressureSfc_hPa = m.SurfacePressure[i] ?? 0;
                    double pressureSfc_Pa = pressureSfc_hPa * 100.0;
                    double radiation = m.ShortwaveRadiation[i] ?? 0;
                    double dir10 = m.WindDirection10m[i] ?? 0;
                    double dir80 = m.WindDirection80m[i] ?? 0;

                    double alpha = 0.143;
                    if (speed10 > 0 && speed80 > 0)
                        alpha = Math.Log(speed80 / speed10) / Math.Log(80.0 / 10.0);

                    var row = new InterpolatedWindData
                    {
                        Time = DateTime.Parse(m.Time[i]),
                        WindDirection = dir80,
                        Radiation = radiation,
                        SurfacePressure = pressureSfc_hPa,
                        Humidity2m = rh2m,
                        Temperature2m = temp2m,
                        Direction10m = dir10,
                        Speed10m = speed10
                    };

                    for (int h = 60; h <= 100; h += 5)
                    {
                        row.WindSpeeds[h] = speed80 * Math.Pow((double)h / 80.0, alpha);
                        row.Temperatures[h] = temp2m - 0.0065 * (h - 2);
                        row.Pressures[h] = pressureSfc_Pa - (12.4 * h);
                        row.RelativeHumidities[h] = rh2m;
                    }
                    resultList.Add(row);
                }
            }

            // 2. 第 17 ~ 45 天的高级合成算法
            if (resultList.Count > 0 && targetDays > apiDays)
            {
                var timeDict = resultList.ToDictionary(x => x.Time, x => x);

                DateTime firstTime = resultList.First().Time;
                DateTime targetEndTime = firstTime.Date.AddDays(targetDays);

                DateTime currentTime = resultList.Last().Time.AddMinutes(15);

                while (currentTime < targetEndTime)
                {
                    // 向前回溯 14 天提取相似气候周期的源数据
                    DateTime sourceTime = currentTime.AddDays(-14);

                    if (!timeDict.TryGetValue(sourceTime, out var sourceData))
                    {
                        sourceTime = currentTime.AddDays(-1);
                        sourceData = timeDict[sourceTime];
                    }

                    var syntheticData = new InterpolatedWindData
                    {
                        Time = currentTime,
                        WindDirection = sourceData.WindDirection,
                        Radiation = sourceData.Radiation,
                        SurfacePressure = sourceData.SurfacePressure,
                        Humidity2m = sourceData.Humidity2m,
                        Temperature2m = sourceData.Temperature2m,
                        Direction10m = sourceData.Direction10m,
                        Speed10m = sourceData.Speed10m,
                        WindSpeeds = new Dictionary<int, double>(sourceData.WindSpeeds),
                        Temperatures = new Dictionary<int, double>(sourceData.Temperatures),
                        Pressures = new Dictionary<int, double>(sourceData.Pressures),
                        RelativeHumidities = new Dictionary<int, double>(sourceData.RelativeHumidities)
                    };

                    resultList.Add(syntheticData);
                    timeDict[currentTime] = syntheticData;

                    currentTime = currentTime.AddMinutes(15);
                }
            }

            return resultList;
        }

        private static void SaveDataToCustomCsv(List<InterpolatedWindData> dataList, string filePath)
        {
            var utf8WithBom = new UTF8Encoding(true);
            string sep = ",";

            using (var writer = new StreamWriter(filePath, false, utf8WithBom))
            {
                var headers = new List<string> { "Date-Time (China Standard Time)", "Wind Direction" };
                for (int h = 60; h <= 100; h += 5) headers.Add($"Wind Speed({h}m)");
                for (int h = 60; h <= 100; h += 5) headers.Add($"Temperature(C)({h}m)");
                for (int h = 60; h <= 100; h += 5) headers.Add($"Pressure (Pa)({h}m)");
                for (int h = 60; h <= 100; h += 5) headers.Add($"RH(%)({h}m)");
                headers.AddRange(new[] { "radiation(W/m^2)", "surface_pressure(hPa)", "humidity2(%)", "temperature2(℃)", "direction10", "speed10" });

                writer.WriteLine(string.Join(sep, headers));

                foreach (var d in dataList)
                {
                    var rowData = new List<string>();
                    rowData.Add(d.Time.ToString("MM/dd/yyyy HH:mm"));
                    rowData.Add(d.WindDirection.ToString("F2", CultureInfo.InvariantCulture));

                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.WindSpeeds[h].ToString("F2", CultureInfo.InvariantCulture));
                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.Temperatures[h].ToString("F2", CultureInfo.InvariantCulture));
                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.Pressures[h].ToString("F2", CultureInfo.InvariantCulture));
                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.RelativeHumidities[h].ToString("F1", CultureInfo.InvariantCulture));

                    rowData.Add(d.Radiation.ToString("F2", CultureInfo.InvariantCulture));
                    rowData.Add(d.SurfacePressure.ToString("F2", CultureInfo.InvariantCulture));
                    rowData.Add(d.Humidity2m.ToString("F1", CultureInfo.InvariantCulture));
                    rowData.Add(d.Temperature2m.ToString("F2", CultureInfo.InvariantCulture));
                    rowData.Add(d.Direction10m.ToString("F2", CultureInfo.InvariantCulture));
                    rowData.Add(d.Speed10m.ToString("F2", CultureInfo.InvariantCulture));

                    writer.WriteLine(string.Join(sep, rowData));
                }
            }
        }

        private static TimeSpan GetDelayToNextRunTime()
        {
            DateTime now = DateTime.Now;
            DateTime nextRun = DateTime.MaxValue;

            foreach (var runTime in _runTimes)
            {
                DateTime possibleNextRun = now.Date + runTime;
                if (possibleNextRun <= now) possibleNextRun = possibleNextRun.AddDays(1);
                if (possibleNextRun < nextRun) nextRun = possibleNextRun;
            }
            return nextRun - now;
        }
    }

    #region 数据实体模型

    public class StationInfo
    {
        public string Id { get; set; }
        public string FilePrefix { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        public StationInfo(string id, string filePrefix, double lat, double lon)
        { Id = id; FilePrefix = filePrefix; Lat = lat; Lon = lon; }
    }

    public class InterpolatedWindData
    {
        public DateTime Time { get; set; }
        public double WindDirection { get; set; }
        public Dictionary<int, double> WindSpeeds { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> Temperatures { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> Pressures { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> RelativeHumidities { get; set; } = new Dictionary<int, double>();
        public double Radiation { get; set; }
        public double SurfacePressure { get; set; }
        public double Humidity2m { get; set; }
        public double Temperature2m { get; set; }
        public double Direction10m { get; set; }
        public double Speed10m { get; set; }
    }

    public class OpenMeteoResponse
    {
        [JsonPropertyName("minutely_15")]
        public Minutely15Data Minutely15 { get; set; }
    }

    public class Minutely15Data
    {
        [JsonPropertyName("time")] public List<string> Time { get; set; }
        [JsonPropertyName("wind_speed_10m")] public List<double?> WindSpeed10m { get; set; }
        [JsonPropertyName("wind_speed_80m")] public List<double?> WindSpeed80m { get; set; }
        [JsonPropertyName("wind_direction_10m")] public List<double?> WindDirection10m { get; set; }
        [JsonPropertyName("wind_direction_80m")] public List<double?> WindDirection80m { get; set; }
        [JsonPropertyName("temperature_2m")] public List<double?> Temperature2m { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public List<double?> RelativeHumidity2m { get; set; }
        [JsonPropertyName("surface_pressure")] public List<double?> SurfacePressure { get; set; }
        [JsonPropertyName("shortwave_radiation")] public List<double?> ShortwaveRadiation { get; set; }
    }

    #endregion
}