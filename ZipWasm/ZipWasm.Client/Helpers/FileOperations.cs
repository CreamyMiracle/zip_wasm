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
    public class FileOperations
    {
        public Dictionary<string, TimeSpan> funcTimes = new Dictionary<string, TimeSpan>();

        public enum SizeUnit
        {
            B,
            KB,
            MB,
            GB,
        }

        public static double ToSize(long bytes, SizeUnit unit)
        {
            switch (unit)
            {
                case SizeUnit.B:
                    return bytes;
                case SizeUnit.KB:
                    return bytes / 1024.0;
                case SizeUnit.MB:
                    return bytes / (1024.0 * 1024.0);
                case SizeUnit.GB:
                    return bytes / (1024.0 * 1024.0 * 1024.0);
                default:
                    throw new ArgumentException("Unsupported size unit", nameof(unit));
            }
        }

        public string GetDuration(string path)
        {
            if (path != null && funcTimes.TryGetValue(path, out TimeSpan duration))
            {
                return Math.Round(duration.TotalMilliseconds, 1) + "ms";
            }
            return "";
        }


        public KeyValuePair<string, TimeSpan> VCDiffEncode(DirectoryInfo? oldDir, DirectoryInfo? newDir)
        {
            if (oldDir == null || newDir == null)
            {
                return new KeyValuePair<string, TimeSpan>("", TimeSpan.Zero);
            }

            DirectoryInfo encodeDir = Directory.CreateDirectory(Path.Combine(oldDir.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(encodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, oldVal);
            }

            var newFiles = newDir.GetFiles();
            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo oldFile in oldDir.GetFiles())
            {
                FileInfo newFile = newFiles.Where(newFile => newFile.Name == oldFile.Name).FirstOrDefault();
                total += VCDiffEncode(oldFile, newFile, encodeDir);
            }
            funcTimes[encodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> VCDiffDecode(DirectoryInfo? oldDir, DirectoryInfo? diffDir)
        {
            if (oldDir == null || diffDir == null)
            {
                return new KeyValuePair<string, TimeSpan>("", TimeSpan.Zero);
            }

            DirectoryInfo decodeDir = Directory.CreateDirectory(Path.Combine(diffDir.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(decodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, oldVal);
            }

            var diffFiles = diffDir.GetFiles();
            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo oldFile in oldDir.GetFiles())
            {
                FileInfo diffFile = diffFiles.Where(diffFile => diffFile.Name == oldFile.Name).FirstOrDefault();
                total += VCDiffDecode(oldFile, diffFile, decodeDir);
            }
            funcTimes[decodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> GZipEncode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(encodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += GZipEncode(file, encodeDir);
            }
            funcTimes[encodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> GZipDecode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(decodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += GZipDecode(file, decodeDir);
            }
            funcTimes[decodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> DeflateEncode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(encodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += DeflateEncode(file, encodeDir);
            }
            funcTimes[encodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> DeflateDecode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(decodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += DeflateDecode(file, decodeDir);
            }
            funcTimes[decodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> BrotliEncode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(encodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += BrotliEncode(file, encodeDir);
            }
            funcTimes[encodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> BrotliDecode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(decodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += BrotliDecode(file, decodeDir);
            }
            funcTimes[decodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> ZstdEncode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo encodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(encodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += ZstdEncode(file, encodeDir);
            }
            funcTimes[encodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(encodeDir.FullName, total);
        }

        public KeyValuePair<string, TimeSpan> ZstdDecode(DirectoryInfo files, DirectoryInfo _ = null)
        {
            DirectoryInfo decodeDir = Directory.CreateDirectory(Path.Combine(files.FullName, MethodBase.GetCurrentMethod().Name));

            if (funcTimes.TryGetValue(decodeDir.FullName, out TimeSpan oldVal))
            {
                return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, oldVal);
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (FileInfo file in files.GetFiles())
            {
                total += ZstdDecode(file, decodeDir);
            }
            funcTimes[decodeDir.FullName] = total;
            return new KeyValuePair<string, TimeSpan>(decodeDir.FullName, total);
        }








        private TimeSpan VCDiffEncode(FileInfo? oldFile, FileInfo? newFile, DirectoryInfo encodeDir)
        {
            if (oldFile == null || newFile == null)
            {
                return TimeSpan.Zero;
            }

            FileInfo diffFile = new FileInfo(Path.Combine(encodeDir.FullName, oldFile.Name));

            using var sourceStream = File.Open(oldFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = File.Open(newFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using VcEncoder coder = new VcEncoder(sourceStream, targetStream, File.Create(diffFile.FullName));
            VCDiffResult encodeRes = coder.Encode();
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan VCDiffDecode(FileInfo? oldFile, FileInfo? diffFile, DirectoryInfo decodeDir)
        {
            if (oldFile == null || diffFile == null)
            {
                return TimeSpan.Zero;
            }

            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, diffFile.Name));

            using var sourceStream = File.Open(oldFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var deltaStream = File.Open(diffFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            using VcDecoder decoder = new VcDecoder(sourceStream, deltaStream, File.Create(encodedFile.FullName));
            VCDiffResult decodeRes = decoder.Decode(out long bytesWritten);
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan GZipEncode(FileInfo file, DirectoryInfo encodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            fileStream.CopyTo(new GZipStream(File.Create(encodedFile.FullName), CompressionMode.Compress));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan GZipDecode(FileInfo file, DirectoryInfo decodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new GZipStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            processedStream.CopyTo(File.Create(encodedFile.FullName));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan DeflateEncode(FileInfo file, DirectoryInfo encodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            fileStream.CopyTo(new DeflateStream(File.Create(encodedFile.FullName), CompressionMode.Compress));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan DeflateDecode(FileInfo file, DirectoryInfo decodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new DeflateStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            processedStream.CopyTo(File.Create(encodedFile.FullName));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan BrotliEncode(FileInfo file, DirectoryInfo encodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            fileStream.CopyTo(new BrotliStream(File.Create(encodedFile.FullName), CompressionMode.Compress));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan BrotliDecode(FileInfo file, DirectoryInfo decodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new BrotliStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            DateTime start = DateTime.Now;
            processedStream.CopyTo(File.Create(encodedFile.FullName));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan ZstdEncode(FileInfo file, DirectoryInfo encodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(encodeDir.FullName, file.Name));

            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            DateTime start = DateTime.Now;
            fileStream.CopyTo(new CompressionStream(File.Create(encodedFile.FullName)));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }

        private TimeSpan ZstdDecode(FileInfo file, DirectoryInfo decodeDir)
        {
            FileInfo encodedFile = new FileInfo(Path.Combine(decodeDir.FullName, file.Name));

            using var processedStream = new DecompressionStream(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

            DateTime start = DateTime.Now;
            processedStream.CopyTo(File.Create(encodedFile.FullName));
            TimeSpan duration = DateTime.Now - start;
            return duration;
        }
    }
}
