﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AssemblerVVM
{
    class pair
    {
        public int start;
        public int end;
        public pair(int one, int two)
        {
            start = one;
            end = two;
        }
    }

    class str_header
    {
        public byte[] signature
        { get; set; }
        public int version
        { get; set; }
        public int const_count
        { get; set; }
        public int size_const
        { get; set; }

        public str_header()
        {
            signature = new byte[2];
            signature[0] = 0xBA;
            signature[1] = 0xBA;
            version = 0100;
        }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(signature);
                    writer.Write(version);
                    writer.Write(const_count);
                    writer.Write(size_const);
                }
                return m.ToArray();
            }
        }
    }

    class funcH_common
    {
        public ushort start_id
        { get; set; }
        public int count_of_funcs
        { get; set; }

        public funcH_common(ushort id, int count)
        {
            start_id = id;
            count_of_funcs = count;
        }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(start_id);
                    writer.Write(count_of_funcs);

                }
                return m.ToArray();
            }
        }
    }

    class funcH_signature
    {
        public int size_func
        { get; set; }
        public int size_bytecode
        { get; set; }
        public int size_signature
        { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(size_func);
                    writer.Write(size_bytecode);
                    writer.Write(size_signature);
                }
                return m.ToArray();
            }
        }
    }

    class funcH_bytecode
    {
        public ushort id
        { get; set; }
        public int count_locals
        { get; set; }
        public int count_args
        { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id);
                    writer.Write(count_locals);
                    writer.Write(count_args);
                }
                return m.ToArray();
            }
        }

        public funcH_bytecode()
        {
            count_locals = 0;
            count_args = 0;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            /*
             *  
             */
            if (args.Length < 1)
            {
                Console.WriteLine("No input files");
                return 1;
            }
            Dictionary<int, string> ConstID_VALUE = new Dictionary<int, string>(); 
            Dictionary<string, int> ConstKEY_ID = new Dictionary<string, int>();
            Dictionary<string, int> CodeMARK_POS = new Dictionary<string, int>();

            List<pair> FuncPos = new List<pair>(); //This array defines first and last line of every procedure

            string outname = args[0].Replace(".vasm","")+".vvm";
            if (args.Length > 1)
                outname = args[1];
            
            int[] pos_d = new int[2] { 0, 0 }; //This array defines first and last line of data segment
            string[] source = System.IO.File.ReadAllLines(args[0]); //Array of source code

            List<string> src = PositionAnalyse(source, ref pos_d, ref FuncPos, ref CodeMARK_POS);
            
            using (var bw = new BinaryWriter(File.Open(outname, FileMode.OpenOrCreate))) //Writing into a file with bytecode
            {
                HeaderAnalyse(src, pos_d, bw, ref ConstID_VALUE, ref ConstKEY_ID); 
                funcH_common FuncCommonH = new funcH_common(CRC16_alg("main"), FuncPos.Count); //We define there that start procedure calls "main"
                bw.Write(FuncCommonH.Serialize()); //We also define there number of procedures in our code
                for (int i = 0; i < FuncPos.Count; i++)
                {
                    FuncAnalyse(src, FuncPos[i], bw, ConstKEY_ID, CodeMARK_POS);
                }
            }

            return 0;
        }

        static List<string> PositionAnalyse(string[] input, ref int[] posD, ref List<pair> posC, ref Dictionary<string, int> marks)
        {
            /*
             * This function fills arrays that define positions of functions and data in source text
             * Return value - is an array of source text without comments, empty strings etc
             * ---
             * input - is an array of source text
             * posD - is an array of start and end position of data segment
             * posC - is an array of start and end position of every procedure
             * marks - is an array of labes
             */
            List<string> src = new List<string>();
            bool func_flag = false;
            int pos1 = 0, pos2 = 0, numline = 0;
            foreach (string s in input)
                if (s != "") //Skip empty strings
                {
                    if (func_flag == true && Regex.IsMatch(s, @"\w+:")) //Labels cannot be outside of procedure
                    {
                        marks.Add(s.Trim(' ', '\t', ':'), numline);
                    }
                    else
                    {
                        src.Add(s.Trim(' ', '\t'));

                        if (s.Contains(".data")) //Checking data segment
                            posD[0] = src.Count - 1;
                        if (s.Contains(".endd"))
                            posD[1] = src.Count - 1;

                        if (s.Contains(".proc")) //Checking procedure segment
                        {
                            numline = 0;
                            pos1 = src.Count - 1;
                            func_flag = true;
                        }

                        if (s.Contains(".endp"))
                        {
                            pos2 = src.Count - 1;
                            if (func_flag == true)
                            {
                                func_flag = false;
                                posC.Add(new pair(pos1, pos2));
                            }
                        }
                        numline++;
                    }
                }
            return src;
        }

        static void HeaderAnalyse(List<string> src, int[] pos, BinaryWriter bw, ref Dictionary<int, string> id_v, ref Dictionary<string, int> k_id)
        {
            /*
             * This function creates bytecode header. Header contains signature, version, text constants and their size
             * ---
             * src - clear source text
             * pos - position of data segment in source text
             * bw - writer to a file
             * id_v, k_id - dictionaries for text constatns
             */
            str_header ConstH = new str_header(); //Object that stores all text consntants.            
            string pattern = "\".*\""; //Pattern to take text constants (to delete)
            string pattern_adv = "\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\""; //Advanced patternn
            int j = 1;
            for (int i = pos[0] + 1; i < pos[1]; i++) //pos[0] = .data ; pos[0]+1 = first text const
            {
                int position = src[i].IndexOf(" ");
                string key = src[i].Substring(0, position);
                string value = Regex.Match(src[i], pattern_adv).ToString();
                value = value.Substring(1, value.Length-2).Replace(@"\n", "\n").Replace(@"\r", "\r").Replace("\\\"","\"") + "\0"; 
                id_v.Add(j, value); k_id.Add(key, j++); //All contstants have their numeric equivalent, so we store both.
                ConstH.const_count++; ConstH.size_const += (value.Length); //Defining total size of constants
            }
            bw.Write(ConstH.Serialize());
            for (int i = 1; i < j; i++)
            {
                bw.Write(Encoding.ASCII.GetBytes(id_v[i]));
            }
        }

        static void FuncAnalyse(List<string> code, pair pos, BinaryWriter bw, Dictionary<string, int> dictStr, Dictionary<string, int> dictJmp)
        {
            /*
             * This function writes an actual procedure in bytecode. 
             * It will decode text-name of instructio into bytecode aswell as agruments for instruction
             * ---
             * code - clear source code
             * pos - position of procedures
             * dicStr - dictionary for text constants
             * dictJmp - dictionary for every jump
             */ 
            string name = "";
            MemoryStream str = new MemoryStream();
            funcH_signature sign = new funcH_signature();
            funcH_bytecode bc = new funcH_bytecode();
            
            string[] current_str = code[pos.start].Split(' '); //Spliting string in case of arguments for instruction
            switch (current_str.Length)
            {
                case 4:  //2 arg instruction
                    bc.count_args = System.Convert.ToInt32(current_str[3]);
                    bc.count_locals = System.Convert.ToInt32(current_str[2]);
                    name = current_str[1];
                    break;
                case 3: //1 arg intruction
                    bc.count_locals = System.Convert.ToInt32(current_str[2]);
                    name = current_str[1];
                    break;

                case 2: //No arg
                    name = current_str[1];  
                    break;
            }
            bc.id = CRC16_alg(name); //Hash encode for function name 
            name += "\0";
            sign.size_signature = name.Length;
            using (BinaryWriter writer = new BinaryWriter(str))
            {
                int j = 1;
                for (int i = pos.start + 1; i < pos.end; i++)
                {
                    current_str = code[i].Split(' ');
                    opcode current_opc = (opcode)Enum.Parse(typeof(opcode), current_str[0].ToUpper());
                    writer.Write((byte)current_opc);

                    if (current_opc == opcode.DLOAD)
                        writer.Write(Convert.ToDouble(current_str[1]));
                    else if (current_opc == opcode.ILOAD)
                        writer.Write(Convert.ToInt64(current_str[1]));
                    else if (current_opc == opcode.SLOAD)
                        writer.Write((ushort)dictStr[current_str[1]]);
                    else if (current_opc == opcode.CALL)
                        writer.Write(CRC16_alg(current_str[1]));
                    else if (threebytes.Contains(current_opc))
                        writer.Write(ushort.Parse(current_str[1]));
                    else if (fivebytes.Contains(current_opc))
                    {
                        writer.Write(CRC16_alg(current_str[1]));
                        writer.Write(ushort.Parse(current_str[2]));
                    }
                    else if (jumps.Contains(current_opc)) //Pain in the arse
                        writer.Write(FindOffset(code, pos, j, ((ushort)dictJmp[current_str[1]]-j)));
                    j++;
                }
            }

            byte[] bcode = str.ToArray();
            sign.size_bytecode = bcode.Length;
            sign.size_func = 22 + sign.size_bytecode + sign.size_signature; //Magic number 22 - size of meta-info for 

            bw.Write(sign.Serialize());
            bw.Write(Encoding.ASCII.GetBytes(name));
            bw.Write(bc.Serialize());
            bw.Write(bcode);
        }

        public static short FindOffset(List<string> code, pair pos, int curr_pos, int off)
        {
            /*
             * This function calculating offset of bytes to jump a label. 
             */
            short result = 0;
            if (off > 0) //Jumping forward
            {
                for (int i = curr_pos + 1; i < curr_pos + off; i++)
                {
                    result += OpCodeSize((opcode)Enum.Parse(typeof(opcode), code[pos.start+i].Split(' ')[0].ToUpper()));
                }
            }
            else //Jumping backward
            {
                for (int i = curr_pos; i >= curr_pos + off; i--)
                {
                    result -= OpCodeSize((opcode)Enum.Parse(typeof(opcode), code[pos.start+i].Split(' ')[0].ToUpper()));
                }
            }
            return result;
        }

        public static short OpCodeSize(opcode opc)
        {
            short result = 0;
            if (jumps.Contains(opc) || threebytes.Contains(opc))
                result += 3;
            else if (fivebytes.Contains(opc))
                result += 5;
            else if (ninebytes.Contains(opc))
                result += 9;
            else result++;
            return result;

        }

        public static ushort CRC16_alg(string msg)
        {
            /*
             * HashFunction on Cyclic redundacy check algorythm
             */
            byte[] text = Encoding.ASCII.GetBytes(msg);
            const ushort polinom = 0xa001;
            ushort code = 0xffff;

            for (int i = 0, size = text.Length; i < size; ++i)
            {
                code ^= (ushort)(text[i] << 8);

                for (uint j = 0; j < 8; ++j)
                {
                    code >>= 1;
                    if ((code & 0x01) != 0) code ^= polinom;
                }
            }

            return code;
        }

        /*static List<opcode> onebyte = new List<opcode>{opcode.INVALID, opcode.DLOAD0, opcode.ILOAD0, opcode.SLOAD0, opcode.DLOAD1, opcode.ILOAD1, opcode.DLOADM1,
            opcode.ILOADM1, opcode.DADD, opcode.IADD, opcode.DSUB, opcode.ISUB, opcode.DMUL, opcode.IMUL, opcode.DDIV, opcode.IDIV, opcode.IMOD, opcode.DNEG, 
            opcode.INEG, opcode.IAOR, opcode.IAAND, opcode.IAXOR, opcode.IPRINT, opcode.DPRINT, opcode.SPRINT, opcode.I2D, opcode.D2I, opcode.S2I, opcode.SWAP,
            opcode.POP, opcode.LOADDVAR0, opcode.LOADDVAR1, opcode.LOADDVAR2, opcode.LOADDVAR3, opcode.LOADIVAR0, opcode.LOADIVAR1, opcode.LOADIVAR2, opcode.LOADIVAR3,
            opcode.LOADSVAR0, opcode.LOADSVAR1, opcode.LOADSVAR2, opcode.LOADSVAR3, opcode.STOREDVAR0, opcode.STOREDVAR1, opcode.STOREDVAR2, opcode.STOREDVAR3,
            opcode.STOREIVAR0, opcode.STOREIVAR1, opcode.STOREIVAR2, opcode.STOREIVAR3, opcode.STORESVAR0, opcode.STORESVAR1, opcode.STORESVAR2, opcode.STORESVAR3,
            opcode.ICMP, opcode.DCMP, opcode.DUMP, opcode.STOP, opcode.RETURN, opcode.BREAK};*/

        static List<opcode> ninebytes = new List<opcode> { opcode.DLOAD, opcode.ILOAD };

        static List<opcode> threebytes = new List<opcode> { opcode.LOADDVAR, opcode.LOADIVAR, opcode.LOADSVAR, opcode.STOREDVAR,
            opcode.STOREIVAR, opcode.STORESVAR, opcode.SLOAD, opcode.CALL};

        static List<opcode> fivebytes = new List<opcode> {opcode.LOADCTXDVAR, opcode.LOADCTXIVAR, opcode.LOADCTXSVAR, opcode.STORECTXDVAR,
            opcode.STORECTXIVAR, opcode.STORECTXSVAR};

        static List<opcode> jumps = new List<opcode> {opcode.JA, opcode.IFICMPE, opcode.IFICMPG, opcode.IFICMPGE, opcode.IFICMPL,
            opcode.IFICMPLE, opcode.IFICMPNE};
    }

    enum opcode
    {
        INVALID,
        DLOAD,
        ILOAD,
        SLOAD,
        DLOAD0,
        ILOAD0,
        SLOAD0,
        DLOAD1,
        ILOAD1,
        DLOADM1,
        ILOADM1,
        DADD,
        IADD,
        DSUB,
        ISUB,
        DMUL,
        IMUL,
        DDIV,
        IDIV,
        IMOD,
        DNEG,
        INEG,
        IAOR,
        IAAND,
        IAXOR,
        IPRINT,
        DPRINT,
        SPRINT,
        I2D,
        D2I,
        S2I,
        SWAP,
        POP,
        LOADDVAR0,
        LOADDVAR1,
        LOADDVAR2,
        LOADDVAR3,
        LOADIVAR0,
        LOADIVAR1,
        LOADIVAR2,
        LOADIVAR3,
        LOADSVAR0,
        LOADSVAR1,
        LOADSVAR2,
        LOADSVAR3,
        STOREDVAR0,
        STOREDVAR1,
        STOREDVAR2,
        STOREDVAR3,
        STOREIVAR0,
        STOREIVAR1,
        STOREIVAR2,
        STOREIVAR3,
        STORESVAR0,
        STORESVAR1,
        STORESVAR2,
        STORESVAR3,
        LOADDVAR,
        LOADIVAR,
        LOADSVAR,
        STOREDVAR,
        STOREIVAR,
        STORESVAR,
        LOADCTXDVAR,
        LOADCTXIVAR,
        LOADCTXSVAR,
        STORECTXDVAR,
        STORECTXIVAR,
        STORECTXSVAR,
        DCMP,
        ICMP,
        JA,
        IFICMPNE,
        IFICMPE,
        IFICMPG,
        IFICMPGE,
        IFICMPL,
        IFICMPLE,
        DUMP,
        STOP,
        CALL,
        RETURN,
        BREAK
    };
}
