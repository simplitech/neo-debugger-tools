using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Debugger.Profiler
{
    public class SourceFileLine
    {
        public string fileName;
        public int lineNumber;

        public SourceFileLine(string filename, int lineno)
        {
            fileName = filename;
            lineNumber = lineno;
        }
    }

    public class SourceStmtInfo
    {
        private const int MAXNOPTCODES = 256;

        public SourceFileLine _filelineo;
        public string _sourceStmt;
        public int[] _stmtOpcodeCount = new int[MAXNOPTCODES];
        public decimal[] _stmtOpcodeCost = new decimal[MAXNOPTCODES];

        public Dictionary<string, int> _sysCallCount = new Dictionary<string, int>();
        public Dictionary<string, decimal> _sysCallCost= new Dictionary<string, decimal>();
    }

    public class ProfilerContext
    {
        private const int MAXNOPTCODES = 256;

        public Dictionary<string, string[]> sourceLines = new Dictionary<string, string[]>();

        public string[] opcodeNames = new string[MAXNOPTCODES];
        public decimal[] opcodeCosts = new decimal[MAXNOPTCODES];
        public bool[] opcodeUsed = new bool[MAXNOPTCODES];
        public Dictionary<string, SourceStmtInfo> dictStmtInfo;
        public int[] totalTallyByOpcode = new int[MAXNOPTCODES];
        public decimal[] totalCostByOpcode = new decimal[MAXNOPTCODES];
        public HashSet<string> sysCallUsed = new HashSet<string>();

        public ProfilerContext()
        {
            dictStmtInfo = new Dictionary<string, SourceStmtInfo>();
        }

        public void TallyOpcode(Neo.VM.OpCode opcode, decimal opCost, int lineNumber, string fileName, string fileSource, string sysCall)
        {
            SourceFileLine sfl = new SourceFileLine(fileName, lineNumber);

            if (!opcodeUsed[(int)opcode])
            {
                opcodeCosts[(int)opcode] = opcode == VM.OpCode.SYSCALL ? 0: opCost;
                opcodeNames[(int)opcode] = opcode.ToString();
                opcodeUsed[(int)opcode] = true;
            }

            string[] lines;
            if (sourceLines.ContainsKey(fileName))
            {
                lines = sourceLines[fileName];
            }
            else
            {
                lines = fileSource.Split('\n');
                sourceLines[fileName] = lines;                
            }

            var lineSource = (lineNumber >= 0 && lineNumber < lines.Length) ? lines[lineNumber] : "// No source code available";

            string key = fileName + ":" + lineNumber.ToString();

            SourceStmtInfo ssi;
            if (dictStmtInfo.Keys.Contains(key))
            {
                ssi = dictStmtInfo[key];
            }
            else
            {
                ssi = new SourceStmtInfo();
                ssi._filelineo = sfl;
                ssi._sourceStmt = lineSource;
                ssi._stmtOpcodeCount[(int)opcode] = 0;
                dictStmtInfo[key] = ssi;
            }

            if (opcode == VM.OpCode.SYSCALL && sysCall != null)
            {
                sysCall = sysCall.Replace("Neo.", "");
                sysCallUsed.Add(sysCall);

                if (!ssi._sysCallCost.ContainsKey(sysCall))
                {
                    ssi._sysCallCost[sysCall] = 0;
                    ssi._sysCallCount[sysCall] = 0;
                }

                ssi._sysCallCount[sysCall] += 1;
                ssi._sysCallCost[sysCall] += opCost;
            }

            ssi._stmtOpcodeCount[(int)opcode]++;
            ssi._stmtOpcodeCost[(int)opcode] = ssi._stmtOpcodeCount[(int)opcode] * opcodeCosts[(int)opcode];

        }

        public Exception DumpCSV(string avmFilePath)
        {
            string csvfilename = avmFilePath.Replace(".avm", ".csv");
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(csvfilename))
                {
                    file.WriteLine();
                    file.WriteLine("\"Tally by Opcode\"");
                    file.WriteLine();

                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode (Hex) >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"0x" + opcode.ToString("X2") + "\"");
                        }
                    }
                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeNames[opcode] + "\"");
                        }
                    }

                    foreach (var sysCall in sysCallUsed)
                    {
                        file.Write(",\"" + sysCall + "\"");
                    }

                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Cost >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeCosts[opcode].ToString() + "\"");
                        }
                        totalTallyByOpcode[opcode] = 0;
                    }
                    file.WriteLine();

                    var entries = dictStmtInfo.Values.OrderBy(x => x._filelineo.fileName).ThenBy(x => x._filelineo.lineNumber);

                    foreach (SourceStmtInfo ssi in entries)
                    {
                        file.Write("\"" + ssi._filelineo.fileName + "\"");
                        file.Write(",\"" + ssi._filelineo.lineNumber.ToString() + "\"");
                        file.Write(",\"" + ssi._sourceStmt.Replace("\"", "''") + "\"");

                        for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                file.Write(",\"" + ssi._stmtOpcodeCount[opcode].ToString() + "\"");

                                totalTallyByOpcode[opcode] += ssi._stmtOpcodeCount[opcode];
                            }
                        }

                        foreach (var sysCall in sysCallUsed)
                        {
                            int count  = ssi._sysCallCount.ContainsKey(sysCall) ? ssi._sysCallCount[sysCall] :0;
                            file.Write(",\"" + count + "\"");
                        }

                        file.WriteLine();
                    }
                    int totalOpcodeTally = 0;
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Tally by Opcode>" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + totalTallyByOpcode[opcode].ToString() + "\"");
                            totalOpcodeTally += totalTallyByOpcode[opcode];
                        }
                    }
                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Total Tally>" + "\"");
                    file.Write(",\"" + totalOpcodeTally.ToString() + "\"");
                    file.WriteLine();

                    file.WriteLine();
                    file.WriteLine("\"Costs by Opcode\"");
                    file.WriteLine();

                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode (Hex) >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"0x" + opcode.ToString("X2") + "\"");
                        }
                    }
                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeNames[opcode] + "\"");
                        }
                    }
                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Cost >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeCosts[opcode].ToString() + "\"");
                        }
                        totalCostByOpcode[opcode] = 0;
                    }
                    file.WriteLine();

                    foreach (SourceStmtInfo ssi in dictStmtInfo.Values)
                    {
                        file.Write("\"" + ssi._filelineo.fileName + "\"");
                        file.Write(",\"" + ssi._filelineo.lineNumber.ToString() + "\"");
                        file.Write(",\"" + ssi._sourceStmt.Replace("\"", "''") + "\"");

                        for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                file.Write(",\"" + ssi._stmtOpcodeCost[opcode].ToString() + "\"");

                                totalCostByOpcode[opcode] += ssi._stmtOpcodeCost[opcode];
                            }
                        }

                        foreach (var sysCall in sysCallUsed)
                        {
                            decimal cost = ssi._sysCallCost.ContainsKey(sysCall) ? ssi._sysCallCost[sysCall]: 0;
                            file.Write(",\"" + cost + "\"");
                        }

                        file.WriteLine();
                    }
                    decimal totalOpcodeCost = 0;
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Costs by Opcode>" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + totalCostByOpcode[opcode].ToString() + "\"");
                            totalOpcodeCost += totalCostByOpcode[opcode];
                        }
                    }
                    file.WriteLine();
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Total Cost>" + "\"");
                    file.Write(",\"" + totalOpcodeCost.ToString() + "\"");
                    file.WriteLine();
                }
            }
            catch(Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}
