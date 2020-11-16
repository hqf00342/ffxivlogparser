using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ffxivlogparse
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }

            var debug = false;
            var target = args[0];

            if (target == "-v" && args.Length > 1)
            {
                target = args[1];
                debug = true;
            }

            if (File.Exists(target))
            {
                AnalyzeFF14LogFile(target, debug);
            }
            else if (Directory.Exists(target))
            {
                foreach (var filename in Directory.EnumerateFiles(target).OrderBy(a => new FileInfo(a).LastWriteTime))
                {
                    AnalyzeFF14LogFile(filename, debug);
                }
            }
            else
            {
                Console.WriteLine($"File/Directory not found : {target}");
                Help();
            }
        }

        private static void Help()
        {
            Console.WriteLine("FFXIV log parse and viewer");
            Console.WriteLine("Usage:");
            Console.WriteLine("  ff14logview [-v] <filename>        Parse one logfile of ffxiv");
            Console.WriteLine("  ff14logview [-v] <directory name>  Parse all logfiles of ffxiv log dir.");
            Console.WriteLine();
            Console.WriteLine("  -v option shows the trimmed binary message in hexadecimal.");
        }

        private static void AnalyzeFF14LogFile(string filename, bool debug = false)
        {
            Console.WriteLine(filename);

            using (var f = File.OpenRead(filename))
            using (var b = new BinaryReader(f))
            {
                //Start and end index values of logs recorded in this file.
                var startIndex = b.ReadInt32();
                var endIndex = b.ReadInt32();

                //The number of pointers.(same as the number of messages)
                var headerNum = endIndex - startIndex;

                //Get a list of pointers to messages.
                var headerList = new List<Int32>(headerNum);
                for (int i = 0; i < headerNum; i++)
                {
                    headerList.Add(b.ReadInt32());
                }

                //The next address is the offset of the set of log message.
                int offset = (headerNum * 4) + 8; // 8 = startindex(4byte) + endindex(4byte)

                //Loop through all log messages
                for (int ix = 0; ix < headerList.Count; ix++)
                {
                    //Seek to the log item.
                    var startPos = (ix == 0) ? offset : offset + headerList[ix - 1];
                    var length = (ix == 0) ? headerList[ix] : headerList[ix] - headerList[ix - 1];
                    f.Seek(startPos, SeekOrigin.Begin);
                    var rawData = b.ReadBytes(length);

                    //+0:UNIX timestamp
                    var timestamp = BitConverter.ToInt32(rawData, 0);
                    var localTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

                    //+4:logtype
                    var logType = rawData[4];

                    //+5:param
                    var param = rawData[5];

                    //+6: seems to be fixed values: "00 00 1f 1f" or "00 00 1f" 
                    var skipLen = (rawData[9] == 0x1f) ? 10 : 9;

                    //The rest is a UTF-8 text message mix with binaries.
                    var message = rawData.Skip(skipLen).ToArray();
                    var str = ParseMessage(message, debug);

                    //Display one line of parsed log.
                    Console.WriteLine($"{ix:D4} {localTime:MM/dd HH:mm:ss} [{logType:x2},{param:x2}] {str}");
                }
            }
        }

        /// <summary>
        /// Create a readable messages from the ffxiv log message.
        /// </summary>
        /// <param name="rawData">ffxiv log message.This is a utf-8 format string mixed with binaries</param>
        /// <returns>readable log message.</returns>
        private static string ParseMessage(in byte[] rawData, bool debug = false)
        {
            if (rawData == null || rawData.Length == 0)
                return string.Empty;

            //Debug character for FF14's specific symbols.
            const byte ALT_SYMBOL = (byte)'*';

            var len = rawData.Length;
            var outBuf = new List<byte>();

            for (int i = 0; i < rawData.Length; i++)
            {
                // pre-fetch 4 bytes.
                byte c1 = rawData[i];
                byte c2 = (len <= i + 1) ? (byte)0 : rawData[i + 1];
                byte c3 = (len <= i + 2) ? (byte)0 : rawData[i + 2];
                byte c4 = (len <= i + 3) ? (byte)0 : rawData[i + 3];
                var utf3 = (c1 * 0x10000) + (c2 * 0x100) + c3;

                if (c1 == 2)
                {
                    // FFXIV binary message part.
                    // This part will be skipped as un-readable.
                    // Data format:
                    //   c1 = 0x02
                    //   c2 = opcode(0x12,0x13,0x27,0x2e)
                    //   c3 = length of data.
                    //   data: this seems to always end with 0x03.

                    var startPos = i;

                    //skip this part.
                    i += c3 + 2;

                    if (debug)
                    {
                        var skipdata = rawData.Skip(startPos).Take(c3).ToArray();
                        outBuf.AddRange($"[{BitConverter.ToString(skipdata)}]".Select(c => (byte)c));
                        System.Diagnostics.Debug.WriteLine(BitConverter.ToString(skipdata));
                    }
                }
                else if (c1 >= 0x20 && c1 < 0x7f)
                {
                    //UTF-8 1byte character.
                    outBuf.Add(c1);
                }
                else if (c1 >= 0xc0 && c1 < 0xdf)
                {
                    //UTF8 2bytes character.
                    if (u8char2(c2))
                    {
                        outBuf.Add(c1);
                        outBuf.Add(c2);
                        i++;
                    }
                }
                else if (utf3 >= 0xee80a0 && utf3 <= 0xee839b)
                {
                    // UTF8 3bytes character of private use areas
                    // These characters are used as specific symbols in FFXIV.
                    if (u8char2(c2) && u8char2(c3))
                    {
                        if (debug)
                        {
                            outBuf.Add(ALT_SYMBOL);
                        }
                        i += 2;
                    }
                }
                else if (c1 >= 0xe0 && c1 <= 0xef)
                {
                    // UTF8 3bytes character
                    if (u8char2(c2) && u8char2(c3))
                    {
                        outBuf.Add(c1);
                        outBuf.Add(c2);
                        outBuf.Add(c3);
                        i += 2;
                    }
                }
                else if (c1 >= 0xf0 && c1 <= 0xf4)
                {
                    // UTF8 4bytes character
                    // I have never seen this in ffxiv log
                    if (u8char2(c2) && u8char2(c3) && u8char2(c4))
                    {
                        outBuf.Add(c1);
                        outBuf.Add(c2);
                        outBuf.Add(c3);
                        outBuf.Add(c4);
                        i += 3;
                    }
                }
                else
                {
                    // Skip if the char(c1) has following values:
                    //   c1 < 0x20 (control chars)
                    //   c1 == 0x7f (DEL)
                    //   c1 is >=0x80 and <=0xbf(utf8 2nd char)
                    //   c1 > 0xf4 (out of utf-8 range)
                }
            }

            return Encoding.UTF8.GetString(outBuf.ToArray());

            //Check if it is the 2nd or subsequent byte of UTF-8 characters.
            bool u8char2(byte c) => c >= 0x80 && c <= 0xbf;
        }
    }
}
