using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TestConsole;

const string appServiceUrlFormat = "https://as-serverlessworkshop.azurewebsites.net/api/prime/between/{0}/{1}";
const string consumptionFormat = "https://fa-consumption-serverlessworkshop.azurewebsites.net/api/between/{0}/{1}?code=i4TF0aqfYUL5bSfJL4Qg1gXXl9g03HLKp1Ap3jtPWjMHaJtxJvjWHA==";
const string dedicatedFormat = "https://fa-dedicated-serverlessworkshop.azurewebsites.net/api/between/{0}/{1}?code=EzonZL5GDhbP1aQ/ey/tY10R43xCJCc8Wfv7VfpQdwCR0ZChpDWCew==";

var results = new Dictionary<int, Dictionary<string, List<Measurement>>>();

var config = default(Config?);
while ((config = ReadConfig()) is not null)
{
    Console.WriteLine(config);
    Console.WriteLine();
    Console.WriteLine("Input the amount of requests or 'exit' to quit:");
    while (true)
    {
        var input = Console.ReadLine()?.Trim();
        if (int.TryParse(input?.Trim(), out var count))
        {
            if(config.Targets.Contains(TestTarget.AppService))
                await Measure(TestTarget.AppService, string.Format(appServiceUrlFormat, config.LowerBound, config.UpperBound), count, ConsoleColor.Cyan);

            if (config.Targets.Contains(TestTarget.Consumption))
                await Measure(TestTarget.Consumption, string.Format(consumptionFormat, config.LowerBound, config.UpperBound), count, ConsoleColor.Yellow);

            if (config.Targets.Contains(TestTarget.DedicatedPlan))
                await Measure(TestTarget.DedicatedPlan, string.Format(dedicatedFormat, config.LowerBound, config.UpperBound), count, ConsoleColor.Green);

            Console.WriteLine("\nInput another amount or 'exit' to quit:");
            continue;
        }

        if (input is not null && input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            CreateExcel();
            break;
        }

        Console.WriteLine("Input should be a valid integer or 'exit' to quit the program");
    }
}

static void Write(ConsoleColor color, string message) => Print(color, message, Console.Write);
static void WriteLine(ConsoleColor color, string message) => Print(color, message, Console.WriteLine);

static void Print(ConsoleColor color, string message, Action<object> print)
{
    var c = Console.ForegroundColor;
    Console.ForegroundColor = color;
    print(message);
    Console.ForegroundColor = c;
}

static Config? ReadConfig()
{
    var targets = new List<TestTarget>();
    Console.WriteLine("Choose your specifications:");
    Console.WriteLine("Target the AppService?");
    if (ReadBool()) targets.Add(TestTarget.AppService);
    Console.WriteLine("\nTarget the consumption function?");
    if (ReadBool()) targets.Add(TestTarget.Consumption);
    Console.WriteLine("\nTarget the function with dedicated app service plan?");
    if (ReadBool()) targets.Add(TestTarget.DedicatedPlan);
    Console.WriteLine("\nInput a lower bound:");
    var lower = ReadInt();
    if (lower is null) return null;
    Console.WriteLine("Input an upper bound:");
    var upper = ReadInt();
    return upper is null
        ? null
        : new Config(lower.Value, upper.Value, targets.ToArray());

    static int? ReadInt()
    {
        while (true)
        {
            var line = Console.ReadLine()?.Trim() ?? string.Empty;
            if (int.TryParse(line, out var i)) return i;
            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) return null;
            WriteLine(ConsoleColor.Red, "Input is not numeric. Input a number or 'exit'.");
        }
    }
    static bool ReadBool() => Console.ReadKey() switch
    {
        { KeyChar: 'y' } => true,
        { KeyChar: 'Y' } => true,
        _ => false
    };
}

void CreateExcel()
{
    if (results.Count == 0) return;
    var culture = CultureInfo.CreateSpecificCulture("de-DE");
    var pattern = DateTimeFormatInfo.GetInstance(culture).LongTimePattern + ".fffffff";
    var file = new FileInfo(Path.GetTempFileName());
    var destFileName = file.FullName.Substring(0, file.FullName.LastIndexOf('.')) + ".xlsx";
    file.MoveTo(destFileName);
    var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Write);

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    using var excel = new ExcelPackage(stream);
    foreach (var (count, targets) in results)
    {
        var ws = excel.Workbook.Worksheets.Add($"{count:D} requests");
        var rowIndex = 0;
        foreach (var (target, measurements) in targets)
        {
            rowIndex++;
            var title = ws.Cells[rowIndex, 1, rowIndex, 5];
            title.Merge = true;
            title.Value = new Uri(target).Host;
            title.Style.Font.Bold = true;
            title.Style.Font.Size = 12;
            title.Style.HorizontalAlignment = ExcelHorizontalAlignment.CenterContinuous;

            rowIndex++;
            ws.Cells[rowIndex, 1].Value = "#";
            ws.Cells[rowIndex, 2].Value = "CalledAt";
            ws.Cells[rowIndex, 3].Value = "SendAt";
            ws.Cells[rowIndex, 4].Value = "Delay (ms)";
            ws.Cells[rowIndex, 5].Value = "Elapsed (ms)";

            foreach (var measurement in measurements)
            {
                rowIndex++;
                ws.Cells[rowIndex, 1].Value = measurement.Index;
                ws.Cells[rowIndex, 2].Value = measurement.CalledAt.ToString(pattern, culture);
                ws.Cells[rowIndex, 3].Value = measurement.SendAt.ToString(pattern, culture);
                ws.Cells[rowIndex, 4].Value = measurement.Delay.TotalMilliseconds;
                ws.Cells[rowIndex, 5].Value = measurement.Elapsed.TotalMilliseconds;

                ws.Cells[rowIndex, 2].Style.Numberformat.Format = pattern;
                ws.Cells[rowIndex, 3].Style.Numberformat.Format = pattern;
            }

            rowIndex++;
        }

        ws.Cells[1, 1, rowIndex, 5].AutoFitColumns();
    }

    excel.Save();
    stream.Close();
    Process.Start("C:\\Program Files (x86)\\Microsoft Office\\root\\Office16\\EXCEL.EXE", file.FullName);
}

async Task Measure(TestTarget target, string url, int numberOfRequests, ConsoleColor color)
{
    var uri = new Uri(url);
    if (!results.ContainsKey(numberOfRequests))
    {
        results[numberOfRequests] = new Dictionary<string, List<Measurement>>
        {
            [url] = new(numberOfRequests)
        };
    }

    if (!results[numberOfRequests].ContainsKey(url))
    {
        results[numberOfRequests][url] = new List<Measurement>(numberOfRequests);
    }
    
    const char c = '#';

    WriteLine(color, $"Measuring {numberOfRequests} calls to {target}\n{new string(c, 82)}");

    var tasks = BulkCall(uri, numberOfRequests).ToArray();
    await Task.WhenAll(tasks);
    foreach (var measurement in tasks)
    {
        var m = await measurement;
        WriteLine(color, m.ToString());
        results[numberOfRequests][url].Add(m);
    }
    WriteLine(color, $"{new string(c, 82)}\n");
}

static IEnumerable<Task<Measurement>> BulkCall(Uri uri, int bulkSize)
{
    var calledAt = DateTime.Now;
    return Enumerable.Range(0, bulkSize).Select(SendGetRequest(uri, calledAt)).ToArray();
}

static Func<int, Task<Measurement>> SendGetRequest(Uri uri, DateTime calledAt) =>
    i =>
    {
        Console.Title = $"GET: {uri.Host} {i + 1,+3:D}";
        var client = new HttpClient();
        var sendAt = DateTime.Now;
        var watch = Stopwatch.StartNew();
        return client.GetAsync(uri).ContinueWith(Continuation(i, calledAt, sendAt, watch));
    };

static Func<Task<HttpResponseMessage>, Measurement> Continuation(int i, DateTime calledAt, DateTime sendAt, Stopwatch watch) => _ =>
{
    watch.Stop();
    return new Measurement(i, calledAt, sendAt, watch.Elapsed);
};

namespace TestConsole
{
    public record Measurement(int Index, DateTime CalledAt, DateTime SendAt, TimeSpan Elapsed)
    {
        public TimeSpan Delay = SendAt - CalledAt;
        public override string ToString() =>
            $"{Index,+3:D} {CalledAt:HH:mm:ss.fffffff} {SendAt:HH:mm:ss.fffffff} Delay: {Delay:ss\\.fffffff} Elapsed: {Elapsed:mm\\:ss\\.fffffff}";
    }

    public record Config(int LowerBound, int UpperBound, params TestTarget[] Targets);

    public enum TestTarget
    {
        AppService = 1,
        Consumption = 2,
        DedicatedPlan = 3
    }
}