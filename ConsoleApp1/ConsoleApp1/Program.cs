//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Threading.Tasks;

//namespace WindPowerDataCollector
//{
//    class Program
//    {
//        // 1. 忽略 SSL 证书验证，解决网络代理或企业防火墙拦截问题
//        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
//        {
//            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
//        });

//        // 2. 设定的每天定时执行时间 (上午 08:00 和 下午 16:00)
//        private static readonly TimeSpan[] _runTimes = new TimeSpan[]
//        {
//            new TimeSpan(8, 0, 0),
//            new TimeSpan(16, 0, 0)
//        };

//        // 3. 数据输出的根目录
//        private static readonly string _baseOutputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindDataOutput");

//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("=================================================");
//            Console.WriteLine("   风电场气象数据自动采集系统 (CSV分文件夹版)");
//            Console.WriteLine("=================================================\n");

//            // 初始化所有站点信息 (加入您需要的文件名前缀)
//            var stations = new List<StationInfo>
//            {
//                // 参数依次为: 站点ID, 文件名前缀(如SX_GNZZ_WF), 纬度(Lat), 经度(Lon)
//                new StationInfo("GJNY_SXGS_FSG_FDC_STA", "SX_GNFS_WF",  37.2613,  107.5041),
//                new StationInfo("GJNY_SXGS_ZZ_FDC_STA",  "SX_GNZZ_WF",  37.2423,  108.727211),
//                new StationInfo("GJNY_SXGS_LJS_FDC_STA", "SX_GNLJS_WF", 37.4694,  108.396111),
//                new StationInfo("GJNY_SXGS_JSL_FDC_STA", "SX_GNJSL_WF", 37.27343, 108.511806),
//                new StationInfo("GJNY_SXGS_CSL_FDC_STA", "SX_GNCSL_WF", 37.281,   108.213),
//                new StationInfo("GJNY_SXGS_XZ_FDC_STA",  "SX_GNXZ_WF",  37.4694,  107.8452)
//            };

//            // 程序启动时强制执行一次采集，方便您立即看到结果
//            Console.WriteLine("[系统提示] 程序启动，开始执行首次采集任务...");
//            await FetchAndSaveAllStationsAsync(stations);

//            // 进入定时守护进程模式 (永久循环)
//            while (true)
//            {
//                TimeSpan delay = GetDelayToNextRunTime();
//                DateTime nextRunTime = DateTime.Now.Add(delay);

//                Console.WriteLine($"\n[调度器] 下一次采集任务将在: {nextRunTime:yyyy-MM-dd HH:mm:ss} 执行.");
//                Console.WriteLine($"[调度器] 系统休眠等待 {(int)delay.TotalHours}小时 {delay.Minutes}分钟...\n");

//                await Task.Delay(delay); // 挂起，直到下一个任务时间

//                Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 触发定时采集任务！");
//                await FetchAndSaveAllStationsAsync(stations);
//            }
//        }

//        /// <summary>
//        /// 核心方法：遍历站点、获取数据、创建文件夹、保存CSV
//        /// </summary>
//        private static async Task FetchAndSaveAllStationsAsync(List<StationInfo> stations)
//        {
//            int forecastDays = 14;
//            DateTime now = DateTime.Now;

//            // 每次采集都创建一个独立的新文件夹，如: 批次_20260922_0800
//            string batchFolderName = $"批次_{now:yyyyMMdd_HHmm}";
//            string currentRunDirectory = Path.Combine(_baseOutputDirectory, batchFolderName);

//            if (!Directory.Exists(currentRunDirectory))
//            {
//                Directory.CreateDirectory(currentRunDirectory);
//            }
//            Console.WriteLine($"\n>>> 已创建本轮数据专属文件夹: {batchFolderName}");

//            // 文件名的时间后缀，精确到小时: yyyyMMddHH (例如: 2025071118)
//            string fileTimeSuffix = now.ToString("yyyyMMddHH");

//            foreach (var station in stations)
//            {
//                Console.WriteLine($"-> 正在采集站点: {station.Id}...");
//                try
//                {
//                    // 获取并进行物理气象插值计算
//                    var dataList = await GetAndCalculateWindDataAsync(station.Lat, station.Lon, forecastDays);

//                    if (dataList != null && dataList.Count > 0)
//                    {
//                        // 组装最终的CSV文件名：前缀_yyyyMMddHH.csv (例如: SX_GNZZ_WF_2025071118.csv)
//                        string fileName = $"{station.FilePrefix}_{fileTimeSuffix}.csv";
//                        string filePath = Path.Combine(currentRunDirectory, fileName);

//                        // 存储为标准的 CSV 格式
//                        SaveDataToCustomCsv(dataList, filePath);
//                        Console.WriteLine($"   [成功] 数据已保存至: {fileName}");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.ForegroundColor = ConsoleColor.Red;
//                    Console.WriteLine($"   [错误] 站点 {station.Id} 采集失败: {ex.Message}");
//                    Console.ResetColor();
//                }

//                // 延时1秒，防止请求过快被服务器临时屏蔽
//                await Task.Delay(1000);
//            }
//        }

//        /// <summary>
//        /// 请求 API 并对高空风速、气压、温度进行气象学推导插值
//        /// </summary>
//        private static async Task<List<InterpolatedWindData>> GetAndCalculateWindDataAsync(double lat, double lon, int days)
//        {
//            string url = $"https://api.open-meteo.com/v1/forecast" +
//                         $"?latitude={lat}" +
//                         $"&longitude={lon}" +
//                         $"&minutely_15=wind_speed_10m,wind_speed_80m,wind_direction_10m,wind_direction_80m,temperature_2m,relative_humidity_2m,surface_pressure,shortwave_radiation" +
//                         $"&wind_speed_unit=ms" +
//                         $"&timezone=Asia%2FShanghai" +
//                         $"&forecast_days={days}";

//            HttpResponseMessage response = await _httpClient.GetAsync(url);
//            response.EnsureSuccessStatusCode();

//            string jsonResponse = await response.Content.ReadAsStringAsync();
//            var apiResult = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonResponse);

//            var resultList = new List<InterpolatedWindData>();
//            var m = apiResult?.Minutely15;

//            if (m?.Time != null)
//            {
//                for (int i = 0; i < m.Time.Count; i++)
//                {
//                    double speed10 = m.WindSpeed10m[i] ?? 0;
//                    double speed80 = m.WindSpeed80m[i] ?? 0;
//                    double temp2m = m.Temperature2m[i] ?? 0;
//                    double rh2m = m.RelativeHumidity2m[i] ?? 0;
//                    double pressureSfc_hPa = m.SurfacePressure[i] ?? 0;
//                    double pressureSfc_Pa = pressureSfc_hPa * 100.0;
//                    double radiation = m.ShortwaveRadiation[i] ?? 0;
//                    double dir10 = m.WindDirection10m[i] ?? 0;
//                    double dir80 = m.WindDirection80m[i] ?? 0;

//                    // 计算风切变指数
//                    double alpha = 0.143;
//                    if (speed10 > 0 && speed80 > 0)
//                    {
//                        alpha = Math.Log(speed80 / speed10) / Math.Log(80.0 / 10.0);
//                    }

//                    var row = new InterpolatedWindData
//                    {
//                        Time = DateTime.Parse(m.Time[i]),
//                        WindDirection = dir80,
//                        Radiation = radiation,
//                        SurfacePressure = pressureSfc_hPa,
//                        Humidity2m = rh2m,
//                        Temperature2m = temp2m,
//                        Direction10m = dir10,
//                        Speed10m = speed10
//                    };

//                    // 插值推导 60m ~ 100m (每5米) 的数据
//                    for (int h = 60; h <= 100; h += 5)
//                    {
//                        row.WindSpeeds[h] = speed80 * Math.Pow((double)h / 80.0, alpha);
//                        row.Temperatures[h] = temp2m - 0.0065 * (h - 2);
//                        row.Pressures[h] = pressureSfc_Pa - (12.4 * h); // 使用真实环境气压梯度
//                        row.RelativeHumidities[h] = rh2m;
//                    }
//                    resultList.Add(row);
//                }
//            }
//            return resultList;
//        }

//        /// <summary>
//        /// 将数据按照指定格式写入逗号分隔的 CSV 文件
//        /// </summary>
//        private static void SaveDataToCustomCsv(List<InterpolatedWindData> dataList, string filePath)
//        {
//            // 带 BOM 的 UTF-8，防止 Excel 打开时出现中文乱码
//            var utf8WithBom = new UTF8Encoding(true);

//            // CSV 的核心：逗号分隔符
//            string sep = ",";

//            using (var writer = new StreamWriter(filePath, false, utf8WithBom))
//            {
//                // 1. 动态生成表头
//                var headers = new List<string> { "Date-Time (China Standard Time)", "Wind Direction" };
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Wind Speed({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Temperature(C)({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Pressure (Pa)({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"RH(%)({h}m)");

//                headers.AddRange(new[] { "radiation(W/m^2)", "surface_pressure(hPa)", "humidity2(%)", "temperature2(℃)", "direction10", "speed10" });

//                writer.WriteLine(string.Join(sep, headers));

//                // 2. 逐行写入数据内容
//                foreach (var d in dataList)
//                {
//                    var rowData = new List<string>();

//                    // 日期格式严格遵循您的要求：MM/dd/yyyy HH:mm
//                    rowData.Add(d.Time.ToString("MM/dd/yyyy HH:mm"));

//                    // 使用 CultureInfo.InvariantCulture 确保小数点一定是 "." 而不是 ","，否则会破坏 CSV 结构
//                    rowData.Add(d.WindDirection.ToString("F2", CultureInfo.InvariantCulture));

//                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.WindSpeeds[h].ToString("F2", CultureInfo.InvariantCulture));
//                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.Temperatures[h].ToString("F2", CultureInfo.InvariantCulture));
//                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.Pressures[h].ToString("F2", CultureInfo.InvariantCulture));
//                    for (int h = 60; h <= 100; h += 5) rowData.Add(d.RelativeHumidities[h].ToString("F1", CultureInfo.InvariantCulture));

//                    rowData.Add(d.Radiation.ToString("F2", CultureInfo.InvariantCulture));
//                    rowData.Add(d.SurfacePressure.ToString("F2", CultureInfo.InvariantCulture));
//                    rowData.Add(d.Humidity2m.ToString("F1", CultureInfo.InvariantCulture));
//                    rowData.Add(d.Temperature2m.ToString("F2", CultureInfo.InvariantCulture));
//                    rowData.Add(d.Direction10m.ToString("F2", CultureInfo.InvariantCulture));
//                    rowData.Add(d.Speed10m.ToString("F2", CultureInfo.InvariantCulture));

//                    // 拼接该行所有数据并写入文件
//                    writer.WriteLine(string.Join(sep, rowData));
//                }
//            }
//        }

//        /// <summary>
//        /// 计算距离下一次定时采集还需要休眠多长时间
//        /// </summary>
//        private static TimeSpan GetDelayToNextRunTime()
//        {
//            DateTime now = DateTime.Now;
//            DateTime nextRun = DateTime.MaxValue;

//            foreach (var runTime in _runTimes)
//            {
//                DateTime possibleNextRun = now.Date + runTime;
//                if (possibleNextRun <= now)
//                {
//                    possibleNextRun = possibleNextRun.AddDays(1);
//                }
//                if (possibleNextRun < nextRun)
//                {
//                    nextRun = possibleNextRun;
//                }
//            }
//            return nextRun - now;
//        }
//    }

//    #region 数据实体模型 (Models)

//    public class StationInfo
//    {
//        public string Id { get; set; }
//        public string FilePrefix { get; set; } // 新增：保存文件的前缀
//        public double Lat { get; set; }
//        public double Lon { get; set; }

//        public StationInfo(string id, string filePrefix, double lat, double lon)
//        {
//            Id = id;
//            FilePrefix = filePrefix;
//            Lat = lat;
//            Lon = lon;
//        }
//    }

//    public class InterpolatedWindData
//    {
//        public DateTime Time { get; set; }
//        public double WindDirection { get; set; }

//        public Dictionary<int, double> WindSpeeds { get; set; } = new Dictionary<int, double>();
//        public Dictionary<int, double> Temperatures { get; set; } = new Dictionary<int, double>();
//        public Dictionary<int, double> Pressures { get; set; } = new Dictionary<int, double>();
//        public Dictionary<int, double> RelativeHumidities { get; set; } = new Dictionary<int, double>();

//        public double Radiation { get; set; }
//        public double SurfacePressure { get; set; }
//        public double Humidity2m { get; set; }
//        public double Temperature2m { get; set; }
//        public double Direction10m { get; set; }
//        public double Speed10m { get; set; }
//    }

//    public class OpenMeteoResponse
//    {
//        [JsonPropertyName("minutely_15")]
//        public Minutely15Data Minutely15 { get; set; }
//    }

//    public class Minutely15Data
//    {
//        [JsonPropertyName("time")] public List<string> Time { get; set; }
//        [JsonPropertyName("wind_speed_10m")] public List<double?> WindSpeed10m { get; set; }
//        [JsonPropertyName("wind_speed_80m")] public List<double?> WindSpeed80m { get; set; }
//        [JsonPropertyName("wind_direction_10m")] public List<double?> WindDirection10m { get; set; }
//        [JsonPropertyName("wind_direction_80m")] public List<double?> WindDirection80m { get; set; }
//        [JsonPropertyName("temperature_2m")] public List<double?> Temperature2m { get; set; }
//        [JsonPropertyName("relative_humidity_2m")] public List<double?> RelativeHumidity2m { get; set; }
//        [JsonPropertyName("surface_pressure")] public List<double?> SurfacePressure { get; set; }
//        [JsonPropertyName("shortwave_radiation")] public List<double?> ShortwaveRadiation { get; set; }
//    }

//    #endregion
//}