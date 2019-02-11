using Neo.Lux.Core;
using Neo.Lux.VM;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpCode = Neo.Lux.VM.OpCode;

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
        private const int SYSCALLOPTCODES = 100; // Ten times as many as currently exists

        public SourceFileLine _filelineo;
        public string _sourceStmt;
        public int[] _stmtOpcodeCount = new int[MAXNOPTCODES + SYSCALLOPTCODES];
        public decimal[] _stmtOpcodeCost = new decimal[MAXNOPTCODES + SYSCALLOPTCODES];

        public Dictionary<string, int> _sysCallCount = new Dictionary<string, int>();
        public Dictionary<string, decimal> _sysCallCost= new Dictionary<string, decimal>();
    }

    public class ProfilerContext
    {
        private const int MAXNOPTCODES = 256;
        private const int SYSCALLOPTCODES = 100; // Ten times as many as currently exists

        public Dictionary<string, string[]> sourceLines = new Dictionary<string, string[]>();

        public string[] opcodeNames = new string[MAXNOPTCODES + SYSCALLOPTCODES];
        public decimal[] opcodeCosts = new decimal[MAXNOPTCODES + SYSCALLOPTCODES];
        public bool[] opcodeUsed = new bool[MAXNOPTCODES + SYSCALLOPTCODES];
        public Dictionary<string, SourceStmtInfo> dictStmtInfo;
        public int[] totalTallyByOpcode = new int[MAXNOPTCODES + SYSCALLOPTCODES];
        public decimal[] totalCostByOpcode = new decimal[MAXNOPTCODES + SYSCALLOPTCODES];
        public HashSet<string> sysCallNamesUsed = new HashSet<string>();
        public Dictionary<string, decimal> sysCallCost = new Dictionary<string, decimal>();

        public ProfilerContext()
        {
            dictStmtInfo = new Dictionary<string, SourceStmtInfo>();

            var interopService = new VM.InteropService();
            foreach (var sysCall in interopService.Calls)
            {
                var name = sysCall.name.Replace("Neo.", "");
                sysCallCost[name] = sysCall.gasCost;
            }
        }

        public void TallyOpcode(OpCode opcode, decimal opCost, int lineNumber, string fileName, string fileSource, string sysCallName)
        {
            SourceFileLine sfl = new SourceFileLine(fileName, lineNumber);

            if (!opcodeUsed[(int)opcode])
            {
                opcodeCosts[(int)opcode] = opcode == OpCode.SYSCALL ? 0: opCost;
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

            if (opcode == OpCode.SYSCALL && sysCallName != null)
            {
                sysCallName = sysCallName.Replace("Neo.", "");
                sysCallNamesUsed.Add(sysCallName);

                if (!ssi._sysCallCost.ContainsKey(sysCallName))
                {
                    ssi._sysCallCost[sysCallName] = 0;
                    ssi._sysCallCount[sysCallName] = 0;
                }

                ssi._sysCallCount[sysCallName] += 1;
                ssi._sysCallCost[sysCallName] += opCost;
            }

            ssi._stmtOpcodeCount[(int)opcode]++;
            ssi._stmtOpcodeCost[(int)opcode] = ssi._stmtOpcodeCount[(int)opcode] * opcodeCosts[(int)opcode];

        }

        public Exception DumpCSV(string avmFilePath)
        {
            string csvfilename = avmFilePath.Replace(".avm", ".csv");
            try
            {
                int offset = 0;
                foreach (var sysCallName in sysCallNamesUsed)
                {
                    opcodeUsed[MAXNOPTCODES + offset] = true;
                    opcodeNames[MAXNOPTCODES + offset] = sysCallName;

                    decimal cost = sysCallCost[sysCallName];
                    opcodeCosts[MAXNOPTCODES + offset] = cost;
                    offset++;
                }

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(csvfilename))
                {
                    file.WriteLine();
                    file.WriteLine("\"Tally by Opcode\"");
                    file.WriteLine();

                    // Dump opcodes (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode (Hex) >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"0x" + opcode.ToString("X2") + "\"");
                        }
                    }
                    file.WriteLine();

                    // Dump opcodeNames (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeNames[opcode] + "\"");
                        }
                    }
                    file.WriteLine();

                    // Dump opcodeCosts (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "v Stmt Opcode Tally / Opcode Cost >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeCosts[opcode].ToString() + "\"");
                        }
                        totalTallyByOpcode[opcode] = 0;
                    }
                    file.WriteLine();

                    var entries = dictStmtInfo.Values.OrderBy(x => x._filelineo.fileName).ThenBy(x => x._filelineo.lineNumber);

                    // Dump opcode tally for each (source line x opcode)
                    foreach (SourceStmtInfo ssi in entries)
                    {
                        file.Write("\"" + ssi._filelineo.fileName + "\"");
                        file.Write(",\"" + ssi._filelineo.lineNumber.ToString() + "\"");
                        file.Write(",\"" + ssi._sourceStmt.Replace("\"", "''") + "\"");

                        offset = 0;
                        foreach (var sysCallName in sysCallNamesUsed)
                        {
                            int count = ssi._sysCallCount.ContainsKey(sysCallName) ? ssi._sysCallCount[sysCallName] : 0;
                            ssi._stmtOpcodeCount[MAXNOPTCODES + offset] = count;
                            offset++;
                        }

                        int stmtTally = 0;
                        for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                stmtTally += ssi._stmtOpcodeCount[opcode];
                                totalTallyByOpcode[opcode] += ssi._stmtOpcodeCount[opcode];
                            }
                        }

                        file.Write(",\"" + stmtTally.ToString() + "\""); // Column D = total opcode tally for this stmt

                        for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                file.Write(",\"" + ssi._stmtOpcodeCount[opcode].ToString() + "\"");
                            }
                        }

                        file.WriteLine();
                    }

                    // Tally and dump opcode tallies (footer) / tally grand total tally
                    int totalOpcodeTally = 0;
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Tally by Opcode >" + "\"");
                    file.Write(",\"" + "--------" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + totalTallyByOpcode[opcode].ToString() + "\"");
                            totalOpcodeTally += totalTallyByOpcode[opcode];
                        }
                    }
                    file.WriteLine();

                    // Dump grand total tally (footer)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Total Tally >" + "\"");
                    file.Write(",\"" + totalOpcodeTally.ToString() + "\"");
                    file.WriteLine();

                    file.WriteLine();
                    file.WriteLine("\"Costs by Opcode\"");
                    file.WriteLine();

                    // Dump opcodes (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode (Hex) >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"0x" + opcode.ToString("X2") + "\"");
                        }
                    }
                    file.WriteLine();

                    // Dump opcodeNames (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Opcode >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeNames[opcode] + "\"");
                        }
                    }
                    file.WriteLine();

                    // Dump opcodeCosts (header)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "v Stmt Cost / Opcode Cost >" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + opcodeCosts[opcode].ToString() + "\"");
                        }
                        totalCostByOpcode[opcode] = 0;
                    }
                    file.WriteLine();

                    // Dump opcode cost for each (source line x opcode)
                    foreach (SourceStmtInfo ssi in entries)
                    {
                        file.Write("\"" + ssi._filelineo.fileName + "\"");
                        file.Write(",\"" + ssi._filelineo.lineNumber.ToString() + "\"");
                        file.Write(",\"" + ssi._sourceStmt.Replace("\"", "''") + "\"");

                        offset = 0;
                        foreach (var sysCallName in sysCallNamesUsed)
                        {
                            decimal cost = ssi._sysCallCost.ContainsKey(sysCallName) ? ssi._sysCallCost[sysCallName] : 0;
                            ssi._stmtOpcodeCost[MAXNOPTCODES + offset] = cost;
                            offset++;
                        }

                        decimal stmtCost = 0;
                        for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                stmtCost += ssi._stmtOpcodeCost[opcode];
                                totalCostByOpcode[opcode] += ssi._stmtOpcodeCost[opcode];
                            }
                        }

                        file.Write(",\"" + stmtCost.ToString() + "\""); // Column D = total opcode cost for this stmt

                        for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                        {
                            if (opcodeUsed[opcode])
                            {
                                file.Write(",\"" + ssi._stmtOpcodeCost[opcode].ToString() + "\"");
                            }
                        }
                        file.WriteLine();
                    }

                    // Tally and dump opcode costs (footer) / Tally grad total cost
                    decimal totalOpcodeCost = 0;
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Costs by Opcode >" + "\"");
                    file.Write(",\"" + "--------" + "\"");
                    for (int opcode = 0; opcode < MAXNOPTCODES + SYSCALLOPTCODES; opcode++)
                    {
                        if (opcodeUsed[opcode])
                        {
                            file.Write(",\"" + totalCostByOpcode[opcode].ToString() + "\"");
                            totalOpcodeCost += totalCostByOpcode[opcode];
                        }
                    }
                    file.WriteLine();

                    // Dump grand total cost (footer)
                    file.Write("\"" + "" + "\"");
                    file.Write(",\"" + "" + "\"");
                    file.Write(",\"" + "Total Cost >" + "\"");
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
