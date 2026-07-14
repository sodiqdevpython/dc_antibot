using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dc_antibot.AntiBot.Common
{
    public static class IatScanner
    {
        public static Dictionary<string, List<string>> ScanFor(string filePath, HashSet<string> apisOfInterest)
        {
            var found = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (apisOfInterest == null || apisOfInterest.Count == 0) return found;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return found;

            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Length < 0x40) return found;
                long fileLen = fi.Length;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    if (br.ReadUInt16() != 0x5A4D) return found;          // 'MZ'
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    uint peOffset = br.ReadUInt32();
                    if (peOffset == 0 || peOffset + 24 > fileLen) return found;

                    fs.Seek(peOffset, SeekOrigin.Begin);
                    if (br.ReadUInt32() != 0x00004550) return found;      // 'PE\0\0'

                    br.ReadUInt16();                                       // machine
                    ushort numberOfSections = br.ReadUInt16();
                    fs.Seek(12, SeekOrigin.Current);
                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    fs.Seek(2, SeekOrigin.Current);

                    long optionalHeaderOffset = fs.Position;
                    if (optionalHeaderOffset + sizeOfOptionalHeader > fileLen) return found;

                    ushort magic = br.ReadUInt16();
                    bool is64 = (magic == 0x20B);

                    long importDirOffset = optionalHeaderOffset + (is64 ? 120 : 104);
                    if (importDirOffset + 8 > fileLen) return found;

                    fs.Seek(importDirOffset, SeekOrigin.Begin);
                    uint importRva = br.ReadUInt32();
                    if (importRva == 0) return found;

                    long sectionsOffset = optionalHeaderOffset + sizeOfOptionalHeader;

                    Func<uint, uint> rvaToOffset = (rva) =>
                    {
                        long savedPos = fs.Position;
                        for (int i = 0; i < numberOfSections; i++)
                        {
                            long secOff = sectionsOffset + (i * 40);
                            if (secOff + 40 > fileLen) break;

                            fs.Seek(secOff + 8, SeekOrigin.Begin);
                            uint virtualSize = br.ReadUInt32();
                            uint virtualAddr = br.ReadUInt32();
                            uint rawSize     = br.ReadUInt32();
                            uint rawPointer  = br.ReadUInt32();
                            uint actual = Math.Max(virtualSize, rawSize);
                            if (rva >= virtualAddr && rva < virtualAddr + actual)
                            {
                                fs.Seek(savedPos, SeekOrigin.Begin);
                                return rva - virtualAddr + rawPointer;
                            }
                        }
                        fs.Seek(savedPos, SeekOrigin.Begin);
                        return 0;
                    };

                    uint importOffset = rvaToOffset(importRva);
                    if (importOffset == 0 || importOffset >= fileLen) return found;

                    uint descOff = importOffset;
                    int maxDlls = 500;
                    int dllCount = 0;

                    while (dllCount < maxDlls && descOff + 20 <= fileLen)
                    {
                        fs.Seek(descOff, SeekOrigin.Begin);
                        uint origFirstThunk = br.ReadUInt32();
                        br.ReadUInt32();                                   // timestamp
                        br.ReadUInt32();                                   // forwarder
                        uint nameRva = br.ReadUInt32();
                        uint firstThunk = br.ReadUInt32();

                        if (nameRva == 0) break;
                        uint nameOff = rvaToOffset(nameRva);
                        if (nameOff == 0 || nameOff >= fileLen) break;

                        fs.Seek(nameOff, SeekOrigin.Begin);
                        string dllName = ReadAsciiZ(fs, fileLen, 128);

                        uint thunkRva = origFirstThunk != 0 ? origFirstThunk : firstThunk;
                        uint thunkOff = rvaToOffset(thunkRva);
                        if (thunkOff == 0 || thunkOff >= fileLen) { descOff += 20; dllCount++; continue; }

                        uint pos = thunkOff;
                        int maxFunc = 5000;
                        int fnCount = 0;
                        uint step = is64 ? 8u : 4u;
                        ulong ordinalFlag = is64 ? 0x8000000000000000UL : 0x80000000UL;

                        while (fnCount < maxFunc && pos + step <= fileLen)
                        {
                            fs.Seek(pos, SeekOrigin.Begin);
                            ulong thunk = is64 ? br.ReadUInt64() : br.ReadUInt32();
                            if (thunk == 0) break;

                            if ((thunk & ordinalFlag) == 0)
                            {
                                uint fnRva = (uint)(thunk & 0x7FFFFFFF);
                                uint fnOff = rvaToOffset(fnRva);
                                if (fnOff != 0 && fnOff + 2 < fileLen)
                                {
                                    fs.Seek(fnOff + 2, SeekOrigin.Begin);  // skip Hint
                                    string fnName = ReadAsciiZ(fs, fileLen, 512);
                                    if (!string.IsNullOrEmpty(fnName) &&
                                        apisOfInterest.Contains(fnName.ToLowerInvariant()))
                                    {
                                        List<string> bucket;
                                        if (!found.TryGetValue(dllName, out bucket))
                                        {
                                            bucket = new List<string>();
                                            found[dllName] = bucket;
                                        }
                                        bucket.Add(fnName);
                                    }
                                }
                            }
                            pos += step;
                            fnCount++;
                        }

                        descOff += 20;
                        dllCount++;
                    }
                }
            }
            catch
            {
            }

            return found;
        }

        private static string ReadAsciiZ(FileStream fs, long fileLen, int max)
        {
            var bytes = new List<byte>(max);
            int count = 0;
            while (fs.Position < fileLen && count < max)
            {
                int b = fs.ReadByte();
                if (b <= 0) break;
                bytes.Add((byte)b);
                count++;
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }
    }
}
