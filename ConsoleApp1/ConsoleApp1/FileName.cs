//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Threading.Tasks;

//namespace WindPowerHistoryCollector
//{
//    class Program
//    {
//        // 1. 忽略 SSL 验证，确保网络顺畅
//        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
//        {
//            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
//        })
//        {
//            Timeout = TimeSpan.FromMinutes(5) // 历史数据包可能很大，放宽超时时间至5分钟
//        };

//        private static readonly string _baseOutputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindDataHistory");

//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("=================================================");
//            Console.WriteLine(" 风电场历史气象数据下载器 (时间+高度 双重插值版)");
//            Console.WriteLine("=================================================\n");

//            // ================= 【参数配置区】 =================
//            // 在这里设置您想要获取的历史数据时间段
//            DateTime startDate = new DateTime(2026, 1, 1);   // 开始日期 (例：2026年1月1日)
//            DateTime endDate = new DateTime(2026, 9, 22);   // 结束日期 (例：2026年12月31日)
//            // ==================================================

//            var stations = new List<StationInfo>
//            {
//                new StationInfo("GJNY_SXGS_FSG_FDC_STA", "SX_GNFS_WF",  37.2613,  107.5041),
//                new StationInfo("GJNY_SXGS_ZZ_FDC_STA",  "SX_GNZZ_WF",  37.2423,  108.727211),
//                new StationInfo("GJNY_SXGS_LJS_FDC_STA", "SX_GNLJS_WF", 37.4694,  108.396111),
//                new StationInfo("GJNY_SXGS_JSL_FDC_STA", "SX_GNJSL_WF", 37.27343, 108.511806),
//                new StationInfo("GJNY_SXGS_CSL_FDC_STA", "SX_GNCSL_WF", 37.281,   108.213),
//                new StationInfo("GJNY_SXGS_XZ_FDC_STA",  "SX_GNXZ_WF",  37.4694,  107.8452)
//            };

//            string folderName = $"History_{startDate:yyyyMMdd}_至_{endDate:yyyyMMdd}";
//            string currentRunDirectory = Path.Combine(_baseOutputDirectory, folderName);

//            if (!Directory.Exists(currentRunDirectory))
//            {
//                Directory.CreateDirectory(currentRunDirectory);
//            }
//            Console.WriteLine($"\n>>> 已创建历史数据存档文件夹: {folderName}");

//            foreach (var station in stations)
//            {
//                Console.WriteLine($"\n-> 正在下载并处理站点历史: {station.Id}...");
//                try
//                {
//                    // 核心下载与双重插值
//                    var dataList = await GetHistoricalWindDataAsync(station.Lat, station.Lon, startDate, endDate);

//                    if (dataList != null && dataList.Count > 0)
//                    {
//                        // 生成文件名: SX_GNZZ_WF_History_20230101_20231231.csv
//                        string fileName = $"{station.FilePrefix}_History_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
//                        string filePath = Path.Combine(currentRunDirectory, fileName);

//                        SaveDataToCustomCsv(dataList, filePath);
//                        Console.WriteLine($"   [成功] 已完成 {dataList.Count} 条(15分钟级)数据处理，保存至: {fileName}");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.ForegroundColor = ConsoleColor.Red;
//                    Console.WriteLine($"   [错误] 站点 {station.Id} 处理失败: {ex.Message}");
//                    Console.ResetColor();
//                }

//                await Task.Delay(2000); // 礼貌延时
//            }

//            Console.WriteLine("\n[执行完毕] 所有站点历史数据下载完成！按任意键退出...");
//            Console.ReadKey();
//        }

//        /// <summary>
//        /// 请求历史数据归档 API，并进行时间插值 (1小时->15分钟) 和高度插值 (10m/100m -> 60~100m)
//        /// </summary>
//        private static async Task<List<InterpolatedWindData>> GetHistoricalWindDataAsync(double lat, double lon, DateTime start, DateTime end)
//        {
//            // 注意这里使用的是 archive-api.open-meteo.com 历史归档接口
//            // 由于 ERA5 历史模型提供 100m 而非 80m，我们用 wind_speed_100m 作为高空参考点
//            string url = $"https://archive-api.open-meteo.com/v1/archive" +
//                         $"?latitude={lat}" +
//                         $"&longitude={lon}" +
//                         $"&start_date={start:yyyy-MM-dd}" +
//                         $"&end_date={end:yyyy-MM-dd}" +
//                         $"&hourly=wind_speed_10m,wind_speed_100m,wind_direction_10m,wind_direction_100m,temperature_2m,relative_humidity_2m,surface_pressure,shortwave_radiation" +
//                         $"&wind_speed_unit=ms" +
//                         $"&timezone=Asia%2FShanghai";

//            HttpResponseMessage response = await _httpClient.GetAsync(url);
//            response.EnsureSuccessStatusCode();

//            string jsonResponse = await response.Content.ReadAsStringAsync();
//            var apiResult = JsonSerializer.Deserialize<OpenMeteoArchiveResponse>(jsonResponse);

//            var resultList = new List<InterpolatedWindData>();
//            var hourly = apiResult?.Hourly;

//            if (hourly?.Time != null)
//            {
//                // 遍历每一个小时的数据
//                for (int i = 0; i < hourly.Time.Count; i++)
//                {
//                    // 获取当前小时 (T1) 的原始数据
//                    double ws10_1 = hourly.WindSpeed10m[i] ?? 0;
//                    double ws100_1 = hourly.WindSpeed100m[i] ?? 0;
//                    double wd10_1 = hourly.WindDirection10m[i] ?? 0;
//                    double wd100_1 = hourly.WindDirection100m[i] ?? 0;
//                    double t2m_1 = hourly.Temperature2m[i] ?? 0;
//                    double rh2m_1 = hourly.RelativeHumidity2m[i] ?? 0;
//                    double sp_1 = hourly.SurfacePressure[i] ?? 0;
//                    double rad_1 = hourly.ShortwaveRadiation[i] ?? 0;

//                    // 判断是否生成 15、30、45 分钟的插值 (如果是数组最后一个点，只生成 00 分钟以防越界)
//                    int maxMins = (i == hourly.Time.Count - 1) ? 0 : 45;

//                    for (int m = 0; m <= maxMins; m += 15)
//                    {
//                        double fraction = m / 60.0; // 0, 0.25, 0.5, 0.75
//                        DateTime current15mTime = DateTime.Parse(hourly.Time[i]).AddMinutes(m);

//                        // ================= 核心算法 1：时间维度的平滑插值 =================
//                        double ws10_cur = ws10_1, ws100_cur = ws100_1;
//                        double wd10_cur = wd10_1, wd100_cur = wd100_1;
//                        double t2m_cur = t2m_1, rh2m_cur = rh2m_1, sp_cur = sp_1, rad_cur = rad_1;

//                        if (fraction > 0)
//                        {
//                            // 与下一个小时 (i+1) 的数据进行融合计算
//                            ws10_cur = InterpolateLinear(ws10_1, hourly.WindSpeed10m[i + 1] ?? 0, fraction);
//                            ws100_cur = InterpolateLinear(ws100_1, hourly.WindSpeed100m[i + 1] ?? 0, fraction);
//                            t2m_cur = InterpolateLinear(t2m_1, hourly.Temperature2m[i + 1] ?? 0, fraction);
//                            rh2m_cur = InterpolateLinear(rh2m_1, hourly.RelativeHumidity2m[i + 1] ?? 0, fraction);
//                            sp_cur = InterpolateLinear(sp_1, hourly.SurfacePressure[i + 1] ?? 0, fraction);
//                            rad_cur = InterpolateLinear(rad_1, hourly.ShortwaveRadiation[i + 1] ?? 0, fraction);

//                            // 风向的插值需要特殊处理 (360度环绕问题)
//                            wd10_cur = InterpolateDirection(wd10_1, hourly.WindDirection10m[i + 1] ?? 0, fraction);
//                            wd100_cur = InterpolateDirection(wd100_1, hourly.WindDirection100m[i + 1] ?? 0, fraction);
//                        }

//                        // ================= 核心算法 2：空间高度维度的垂直插值 =================
//                        double alpha = 0.143; // 风切变指数
//                        if (ws10_cur > 0 && ws100_cur > 0)
//                        {
//                            // 通过 10m 和 100m 反推切变系数
//                            alpha = Math.Log(ws100_cur / ws10_cur) / Math.Log(100.0 / 10.0);
//                        }

//                        var row = new InterpolatedWindData
//                        {
//                            Time = current15mTime,
//                            WindDirection = wd100_cur,
//                            Radiation = rad_cur,
//                            SurfacePressure = sp_cur,
//                            Humidity2m = rh2m_cur,
//                            Temperature2m = t2m_cur,
//                            Direction10m = wd10_cur,
//                            Speed10m = ws10_cur
//                        };

//                        double sp_Pa = sp_cur * 100.0;

//                        for (int h = 60; h <= 100; h += 5)
//                        {
//                            row.WindSpeeds[h] = ws100_cur * Math.Pow((double)h / 100.0, alpha); // 高空风速指数律
//                            row.Temperatures[h] = t2m_cur - 0.0065 * (h - 2);
//                            row.Pressures[h] = sp_Pa - (12.4 * h);
//                            row.RelativeHumidities[h] = rh2m_cur;
//                        }

//                        resultList.Add(row);
//                    }
//                }
//            }
//            return resultList;
//        }

//        // --- 辅助数学算法：线性插值 ---
//        private static double InterpolateLinear(double v1, double v2, double fraction)
//        {
//            return v1 + (v2 - v1) * fraction;
//        }

//        // --- 辅助数学算法：风向跨越 360 度圆盘的极坐标插值 ---
//        private static double InterpolateDirection(double d1, double d2, double fraction)
//        {
//            double diff = (d2 - d1 + 360) % 360;
//            if (diff > 180) diff -= 360; // 走最短路径
//            return (d1 + diff * fraction + 360) % 360;
//        }

//        /// <summary>
//        /// 完全兼容您要求的 CSV 导出格式
//        /// </summary>
//        private static void SaveDataToCustomCsv(List<InterpolatedWindData> dataList, string filePath)
//        {
//            var utf8WithBom = new UTF8Encoding(true);
//            string sep = ",";

//            using (var writer = new StreamWriter(filePath, false, utf8WithBom))
//            {
//                var headers = new List<string> { "Date-Time (China Standard Time)", "Wind Direction" };
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Wind Speed({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Temperature(C)({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"Pressure (Pa)({h}m)");
//                for (int h = 60; h <= 100; h += 5) headers.Add($"RH(%)({h}m)");
//                headers.AddRange(new[] { "radiation(W/m^2)", "surface_pressure(hPa)", "humidity2(%)", "temperature2(℃)", "direction10", "speed10" });

//                writer.WriteLine(string.Join(sep, headers));

//                foreach (var d in dataList)
//                {
//                    var rowData = new List<string>();

//                    // 日期格式严格遵循：MM/dd/yyyy HH:mm
//                    rowData.Add(d.Time.ToString("MM/dd/yyyy HH:mm"));
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

//                    writer.WriteLine(string.Join(sep, rowData));
//                }
//            }
//        }
//    }

//    #region 数据实体模型 (Models)

//    public class StationInfo
//    {
//        public string Id { get; set; }
//        public string FilePrefix { get; set; }
//        public double Lat { get; set; }
//        public double Lon { get; set; }

//        public StationInfo(string id, string filePrefix, double lat, double lon)
//        {
//            Id = id; FilePrefix = filePrefix; Lat = lat; Lon = lon;
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

//    public class OpenMeteoArchiveResponse
//    {
//        [JsonPropertyName("hourly")]
//        public HourlyData Hourly { get; set; }
//    }

//    // 历史接口映射对象，包含 100m (因为 ERA5 不原生提供80m)
//    public class HourlyData
//    {
//        [JsonPropertyName("time")] public List<string> Time { get; set; }
//        [JsonPropertyName("wind_speed_10m")] public List<double?> WindSpeed10m { get; set; }
//        [JsonPropertyName("wind_speed_100m")] public List<double?> WindSpeed100m { get; set; }
//        [JsonPropertyName("wind_direction_10m")] public List<double?> WindDirection10m { get; set; }
//        [JsonPropertyName("wind_direction_100m")] public List<double?> WindDirection100m { get; set; }
//        [JsonPropertyName("temperature_2m")] public List<double?> Temperature2m { get; set; }
//        [JsonPropertyName("relative_humidity_2m")] public List<double?> RelativeHumidity2m { get; set; }
//        [JsonPropertyName("surface_pressure")] public List<double?> SurfacePressure { get; set; }
//        [JsonPropertyName("shortwave_radiation")] public List<double?> ShortwaveRadiation { get; set; }
//    }

//    #endregion
//}