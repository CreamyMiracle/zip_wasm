using System;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using VCDiff.Decoders;
using VCDiff.Encoders;
using VCDiff.Includes;
using ZstdNet;
using static MudBlazor.CategoryTypes;
using static ZipWasm.Client.Helpers.FileOperations;

namespace ZipWasm.Client.Helpers
{
    public class Result
    {
        public Result(TimeSpan dur, bool success, double ratio, long size)
        {
            Duration = dur;
            Success = success;
            Ratio = ratio;
            Size = size;
        }

        public double Ratio { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public long Size { get; set; }

        public static Result None()
        {
            return new Result(TimeSpan.Zero, false, 0, 0);
        }
    }

    public class FileOperations
    {
        private Dictionary<string, int> _directories { get; set; } = new Dictionary<string, int>();
        private Dictionary<string, Result> _funcResults = new Dictionary<string, Result>();

        public DirectoryInfo GetNextDir(string? dirPath = null)
        {
            int count = _directories.Count;

            if (dirPath == null)
            {
                dirPath = Path.Combine(Environment.CurrentDirectory, count.ToString());
            }
            DirectoryInfo dir = Directory.CreateDirectory(dirPath);

            _directories.TryAdd(dir.FullName, count);
            return dir;
        }

        public int? GetDirOrder(string? path)
        {
            if (path == null)
            {
                return null;
            }
            _directories.TryGetValue(path, out int order);
            return order;
        }

        public enum SizeUnit
        {
            B,
            KB,
            MB,
            GB,
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1)
            {
                return $"{timeSpan.TotalMilliseconds}ms";
            }
            else if (timeSpan.TotalMinutes < 1)
            {
                return $"{Math.Round(timeSpan.TotalSeconds, 1)}s";
            }
            else if (timeSpan.TotalHours < 1)
            {
                return $"{timeSpan.Minutes}m {Math.Round(timeSpan.TotalSeconds, 0)}s";
            }
            return "ERR";
        }

        public static string ConvertBytesToReadableUnit(long bytes)
        {
            const long KB = 1000;
            const long MB = KB * 1000;
            const long GB = MB * 1000;

            if (bytes >= GB)
            {
                return $"{bytes / (double)GB:F1} GB";
            }
            else if (bytes >= MB)
            {
                return $"{bytes / (double)MB:F1} MB";
            }
            else if (bytes >= KB)
            {
                return $"{bytes / (double)KB:F1} KB";
            }
            else
            {
                return $"{bytes} B";
            }
        }

        public Result GetResult(string path)
        {
            if (path != null && _funcResults.TryGetValue(path, out Result result))
            {
                return result;
            }
            return Result.None();
        }

        public KeyValuePair<string, Result> Diff(DirectoryInfo? srcDir, DirectoryInfo? targetDir)
        {
            if (srcDir == null || targetDir == null)
            {
                return new KeyValuePair<string, Result>("", Result.None());
            }

            DirectoryInfo encodeDir = GetNextDir(Path.Combine(srcDir.FullName, targetDir.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(encodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(encodeDir.FullName, oldVal);
            }

            var newFiles = targetDir.GetFiles();
            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo oldFile in srcDir.GetFiles())
            {
                FileInfo newFile = newFiles.Where(newFile => newFile.Name == oldFile.Name).FirstOrDefault();

                totalStartSize += newFile == null ? 0 : newFile.Length;
                total += VCDiffEncode(oldFile, newFile, encodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[encodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(encodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Un_Diff(DirectoryInfo? srcDir, DirectoryInfo? diffDir)
        {
            if (srcDir == null || diffDir == null)
            {
                return new KeyValuePair<string, Result>("", Result.None());
            }

            DirectoryInfo decodeDir = GetNextDir(Path.Combine(diffDir.FullName, srcDir.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(decodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(decodeDir.FullName, oldVal);
            }

            var diffFiles = diffDir.GetFiles();
            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo oldFile in srcDir.GetFiles())
            {
                FileInfo diffFile = diffFiles.Where(diffFile => diffFile.Name == oldFile.Name).FirstOrDefault();

                totalStartSize += diffFile == null ? 0 : diffFile.Length;
                total += VCDiffDecode(oldFile, diffFile, decodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[decodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(decodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Gzip(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(encodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += GZipEncode(file, encodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[encodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(encodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Un_Gzip(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(decodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += GZipDecode(file, decodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[decodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(decodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Deflate(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(encodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += DeflateEncode(file, encodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[encodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(encodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Un_Deflate(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(decodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += DeflateDecode(file, decodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[decodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(decodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Brotli(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(encodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += BrotliEncode(file, encodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[encodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(encodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Un_Brotli(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(decodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += BrotliDecode(file, decodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[decodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(decodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Zstd(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(encodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += ZstdEncode(file, encodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[encodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(encodeDir.FullName, result);
        }

        public KeyValuePair<string, Result> Un_Zstd(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = GetNextDir(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (_funcResults.TryGetValue(decodeDir.FullName, out Result oldVal))
            {
                return new KeyValuePair<string, Result>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            long totalResultSize = 0;
            long totalStartSize = 0;
            foreach (FileInfo file in files.GetFiles())
            {
                totalStartSize += file.Length;
                total += ZstdDecode(file, decodeDir, ref totalResultSize);
            }

            double ratio = (double)totalResultSize / totalStartSize;
            Result result = new Result(total, true, ratio, totalResultSize);
            _funcResults[decodeDir.FullName] = result;
            return new KeyValuePair<string, Result>(decodeDir.FullName, result);
        }








        private TimeSpan VCDiffEncode(FileInfo? oldFile, FileInfo? newFile, DirectoryInfo encodeDir, ref long resultSize)
        {
            if (oldFile == null || newFile == null)
            {
                return TimeSpan.Zero;
            }

            FileInfo diffFile = new FileInfo(Path.Combine(encodeDir.FullName, oldFile.Name));

            using var sourceStream = File.Open(oldFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = File.Open(newFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(diffFile.FullName);
            using VcEncoder coder = new VcEncoder(sourceStream, targetStream, resultStream);
            VCDiffResult encodeRes = coder.Encode();
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan VCDiffDecode(FileInfo? oldFile, FileInfo? diffFile, DirectoryInfo decodeDir, ref long resultSize)
        {
            if (oldFile == null || diffFile == null)
            {
                return TimeSpan.Zero;
            }

            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, diffFile.Name));

            using var sourceStream = File.Open(oldFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var deltaStream = File.Open(diffFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(encodedFile.FullName);
            using VcDecoder decoder = new VcDecoder(sourceStream, deltaStream, resultStream);
            VCDiffResult decodeRes = decoder.Decode(out long bytesWritten);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan GZipEncode(FileInfo file, DirectoryInfo encodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = new GZipStream(File.Create(encodedFile.FullName), CompressionMode.Compress);
            fileStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.BaseStream.Length;
            return duration;
        }

        private TimeSpan GZipDecode(FileInfo file, DirectoryInfo decodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new GZipStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(encodedFile.FullName);
            processedStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan DeflateEncode(FileInfo file, DirectoryInfo encodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = new DeflateStream(File.Create(encodedFile.FullName), CompressionMode.Compress);
            fileStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.BaseStream.Length;
            return duration;
        }

        private TimeSpan DeflateDecode(FileInfo file, DirectoryInfo decodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new DeflateStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(encodedFile.FullName);
            processedStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan BrotliEncode(FileInfo file, DirectoryInfo encodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = new BrotliStream(File.Create(encodedFile.FullName), CompressionMode.Compress);
            fileStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.BaseStream.Length;
            return duration;
        }

        private TimeSpan BrotliDecode(FileInfo file, DirectoryInfo decodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new BrotliStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(encodedFile.FullName);
            processedStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan ZstdEncode(FileInfo file, DirectoryInfo encodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using var resultStream = new CompressionStream(File.Create(encodedFile.FullName));
            fileStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }

        private TimeSpan ZstdDecode(FileInfo file, DirectoryInfo decodeDir, ref long resultSize)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new DecompressionStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

            DateTime start = DateTime.Now;
            using var resultStream = File.Create(encodedFile.FullName);
            processedStream.CopyTo(resultStream);
            TimeSpan duration = DateTime.Now - start;
            resultSize += resultStream.Length;
            return duration;
        }
    }
}
