﻿/*
    This file is part of PapyrusDotNet.

    PapyrusDotNet is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    PapyrusDotNet is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with PapyrusDotNet.  If not, see <http://www.gnu.org/licenses/>.
	
	Copyright 2014, Karl Patrik Johansson, zerratar@gmail.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PapyrusDotNet.Common
{
    using System.IO;

    using Microsoft.Win32;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class InstallationPath
    {
        public bool SkyrimPathExists
        {
            get
            {
                return !string.IsNullOrEmpty(Skyrim) && System.IO.File.Exists(Skyrim);
            }
        }

        public bool CreationKitPathExists
        {
            get
            {
                return !string.IsNullOrEmpty(CreationKit) && System.IO.File.Exists(CreationKit);
            }
        }

        public string Skyrim { get; set; }
        public string CreationKit { get; set; }
        public InstallationPath(string sp, string ckp)
        {
            if (System.IO.Directory.Exists(sp))
                Skyrim = sp;

            if (System.IO.Directory.Exists(ckp))
                CreationKit = ckp;
        }
    }

    public class Utility
    {


        /*[HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\Shell\MuiCache]
        "D:\\The Elder Scrolls V Skyrim\\CreationKit.exe"="Creation Kit"
        "D:\\The Elder Scrolls V Skyrim\\TESV.exe"="Skyrim"
        "D:\\The Elder Scrolls V Skyrim\\SkyrimLauncher.exe"="Skyrim Launcher"
        "D:\\The Elder Scrolls V Skyrim\\unins000.exe"="Setup/Uninstall"*/


        public static InstallationPath GetInstallationFolder()
        {
            try
            {
                var subLocalSettings = Registry.ClassesRoot.OpenSubKey("Local Settings");
                var subSoftware = subLocalSettings.OpenSubKey("Software");
                var subMicrosoft = subSoftware.OpenSubKey("Microsoft");
                var subWindows = subMicrosoft.OpenSubKey("Windows");
                var subShell = subWindows.OpenSubKey("Shell");
                var subMuiCache = subShell.OpenSubKey("MuiCache");
                var test = subMuiCache.GetValueNames();

                var installationFolder = test.FirstOrDefault(s => s.ToLower().Contains("tesv.exe") || s.ToLower().Contains("skyrimlauncher.exe"));
                var creationkitFolder = test.FirstOrDefault(s => s.ToLower().Contains("creationkit.exe"));

                var ifo = "";
                var ckfo = "";

                if (!String.IsNullOrEmpty(installationFolder))
                    ifo = Path.GetDirectoryName(installationFolder);

                if (!String.IsNullOrEmpty(creationkitFolder))
                    ckfo = Path.GetDirectoryName(creationkitFolder);

                return new InstallationPath(ifo, ckfo);
            }
            catch
            {

            }
            return null;
        }

        public static PapyrusFieldProperties GetFlagsAndProperties(TypeDefinition variable)
        {
            string initialValue = null,
                   docString = null;
            bool isProperty = false,
                isAuto = false,
                isAutoReadOnly = false,
                isHidden = false,
                isConditional = false,
                isGeneric = false;

            if (variable.HasGenericParameters)
            {
                isGeneric = true;
                var genericParameters = variable.GenericParameters;
            }

            foreach (var varAttr in variable.CustomAttributes)
            {
                if (varAttr.AttributeType.Name.Equals("PropertyAttribute"))
                    isProperty = true;

                if (varAttr.AttributeType.Name.Equals("GenericTypeAttribute"))
                {
                    isGeneric = true;
                    foreach (var arg in varAttr.ConstructorArguments)
                    {

                    }
                }

                if (varAttr.AttributeType.Name.Equals("DocStringAttribute"))
                {
                    docString = CustomAttributeValue(varAttr);
                }


                if (varAttr.AttributeType.Name.Equals("InitialValueAttribute"))
                {
                    initialValue = CustomAttributeValue(varAttr);
                }
                if (varAttr.AttributeType.Name.Equals("AutoAttribute"))
                    isAuto = true;

                if (varAttr.AttributeType.Name.Equals("AutoReadOnlyAttribute"))
                    isAutoReadOnly = true;

                if (varAttr.AttributeType.Name.Equals("HiddenAttribute"))
                    isHidden = true;


                if (varAttr.AttributeType.Name.Equals("ConditionalAttribute"))
                    isConditional = true;
            }
            return new PapyrusFieldProperties()
            {
                IsGeneric = isGeneric,
                InitialValue = initialValue,
                IsAuto = isAuto,
                IsAutoReadOnly = isAutoReadOnly,
                IsConditional = isConditional,
                IsHidden = isHidden,
                IsProperty = isProperty,
                DocString = docString
            };
        }

        public static string InitialValue(FieldDefinition variable)
        {
            string initialValue = "None";
            if (variable.InitialValue != null && variable.InitialValue.Length > 0)
            {
                if (variable.FieldType.FullName.ToLower().Contains("system.int")
                    || variable.FieldType.FullName.ToLower().Contains("system.byte")
                    || variable.FieldType.FullName.ToLower().Contains("system.short")
                    || variable.FieldType.FullName.ToLower().Contains("system.long"))
                {
                    initialValue = BitConverter.ToInt32(variable.InitialValue, 0).ToString();
                    // "\"" + Encoding.Default.GetString(v) + "\"";
                }
                if (variable.FieldType.FullName.ToLower().Contains("system.double")
                    || variable.FieldType.FullName.ToLower().Contains("system.float"))
                {
                    initialValue = BitConverter.ToDouble(variable.InitialValue, 0).ToString();
                    // "\"" + Encoding.Default.GetString(v) + "\"";
                }

                if (variable.FieldType.FullName.ToLower().Contains("system.string"))
                {
                    initialValue = "\"" + Encoding.Default.GetString(variable.InitialValue) + "\"";
                }

                if (variable.FieldType.FullName.ToLower().Contains("system.bool"))
                {
                    initialValue = (variable.InitialValue[0] == 1).ToString();
                }
            }
            return initialValue;
        }

        public static string CustomAttributeValue(CustomAttribute varAttr)
        {
            var ctrArg = varAttr.ConstructorArguments.FirstOrDefault();
            if (ctrArg.Value != null)
            {
                if (ctrArg.Value is CustomAttributeArgument)
                {
                    var arg = ((CustomAttributeArgument)ctrArg.Value);
                    var val = arg.Value;

                    return TypeValueConvert(arg.Type.Name, val).ToString();
                }
                return ctrArg.Value.ToString();
            }
            return null;
        }

        public static PapyrusFieldProperties GetFlagsAndProperties(FieldDefinition variable)
        {
            string initialValue = null;
            bool isProperty = false,
                isAuto = false,
                isAutoReadOnly = false,
                isHidden = false,
                isConditional = false,
                isGeneric = false;

            if (variable.FieldType.Name == "T") isGeneric = true;

            foreach (var varAttr in variable.CustomAttributes)
            {
                if (varAttr.AttributeType.Name.Equals("PropertyAttribute"))
                    isProperty = true;
                if (varAttr.AttributeType.Name.Equals("GenericMemberAttribute"))
                {
                    isGeneric = true;
                    foreach (var arg in varAttr.ConstructorArguments)
                    {

                    }
                }
                if (varAttr.AttributeType.Name.Equals("InitialValueAttribute"))
                {
                    var ctrArg = varAttr.ConstructorArguments.FirstOrDefault();
                    if (ctrArg.Value != null)
                    {
                        if (ctrArg.Value is CustomAttributeArgument)
                        {
                            var arg = ((CustomAttributeArgument)ctrArg.Value);
                            var val = arg.Value;

                            initialValue = TypeValueConvert(arg.Type.Name, val).ToString();
                        }
                        else
                            initialValue = ctrArg.Value.ToString();
                    }
                }
                if (varAttr.AttributeType.Name.Equals("AutoAttribute"))
                    isAuto = true;

                if (varAttr.AttributeType.Name.Equals("AutoReadOnlyAttribute"))
                    isAutoReadOnly = true;

                if (varAttr.AttributeType.Name.Equals("HiddenAttribute"))
                    isHidden = true;


                if (varAttr.AttributeType.Name.Equals("ConditionalAttribute"))
                    isConditional = true;
            }
            return new PapyrusFieldProperties()
            {
                IsGeneric = isGeneric,
                InitialValue = initialValue,
                IsAuto = isAuto,
                IsAutoReadOnly = isAutoReadOnly,
                IsConditional = isConditional,
                IsHidden = isHidden,
                IsProperty = isProperty
            };
        }

        public static string GetString(object p)
        {
            if (p is string) return (string)p;
            return "";
        }

        public static bool IsConditional(Code code)
        {
            return code == Code.Ceq || code == Code.Cgt || code == Code.Clt || code == Code.Cgt_Un || code == Code.Clt_Un;
        }

        public static bool IsBranch(Code code)
        {
            return code == Code.Br || code == Code.Br_S || code == Code.Brfalse_S || code == Code.Brtrue_S
                    || code == Code.Brfalse || code == Code.Brtrue;
        }

        public static string GetPapyrusReturnType(TypeReference reference, bool stripGenericMarkers)
        {
            var retType = GetPapyrusReturnType(reference);


            if (stripGenericMarkers && reference.IsGenericInstance)
            {
                retType = retType.Replace("`1", "_" + Utility.GetPapyrusBaseType(reference.FullName.Split('<')[1].Split('>')[0]));
            }

            return retType;
        }

        public static string GetPapyrusReturnType(TypeReference reference)
        {
            return GetPapyrusReturnType(reference.Name, reference.Namespace);
        }

        public static string GetPapyrusBaseType(TypeReference typeRef)
        {
            var Name = typeRef.Name;
            var Namespace = typeRef.Namespace;
            if (Name == "Object" || Namespace.ToLower().Equals("system") || Namespace.ToLower().StartsWith("system.")) return "";

            if (Namespace.ToLower().StartsWith("papyrusdotnet.core.") || Namespace.StartsWith("PapyrusDotNet.System"))
            {
                return "PDN_" + Name;
            }
            if (Namespace.StartsWith("PapyrusDotNet.Core"))
            {
                return typeRef.Name;
            }

            if (String.IsNullOrEmpty(Namespace))
                return Name;

            return Namespace.Replace(".", "_") + "_" + Name;
        }

        public static string GetPapyrusBaseType(string fullName)
        {
            switch (fullName)
            {
                case "Object":
                    return "";
            }

            string Name = fullName;
            string Namespace = "";

            if (fullName.Contains("."))
            {
                Name = fullName.Split('.').LastOrDefault();
                Namespace = fullName.Remove(fullName.LastIndexOf("."));
            }

            if (Name == "Object") return "";

            if (Namespace.ToLower().StartsWith("system"))
            {
                return GetPapyrusReturnType(Name, Namespace);
            }

            if (Namespace.ToLower().StartsWith("papyrusdotnet.core."))
            {
                return "DotNet" + Name;
            }
            if (Namespace.StartsWith("PapyrusDotNet.Core"))
            {
                return Name;
            }

            if (String.IsNullOrEmpty(Namespace))
                return Name;

            return Namespace.Replace(".", "_") + "_" + Name;
        }

        public static int ConvertToTimestamp(DateTime value)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch
            TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            //return the total seconds (which is a UNIX timestamp)
            return (int)span.TotalSeconds;
        }

        public static string GetPapyrusReturnType(string type, string Namespace)
        {

            var swtype = type;
            var swExt = "";
            bool isArray = swtype.Contains("[]");

            if (!String.IsNullOrEmpty(Namespace))
            {
                if (Namespace.ToLower().StartsWith("system"))
                {
                    swExt = "";
                }

                else if (Namespace.ToLower().StartsWith("papyrusdotnet.core."))
                {
                    swExt = "DotNet";
                }
                else
                {
                    if (Namespace.ToLower().Equals("papyrusdotnet.core"))
                        swExt = "";
                    else
                        swExt = Namespace.Replace('.', '_') + "_";
                }
            }
            if (isArray)
            {
                swtype = swtype.Split(new string[] { "[]" }, StringSplitOptions.None)[0];
            }
            switch (swtype.ToLower())
            {
                // for now..
                case "enum": return "";
                case "none":
                case "nil":
                case "nnull":
                case "null":
                case "void":
                    return "None" + (isArray ? "[]" : "");
                case "bool":
                case "boolean":
                    return "Bool" + (isArray ? "[]" : "");
                case "long":
                case "int64":
                case "integer64":
                case "int32":
                case "integer":
                case "integer32":
                    return "Int" + (isArray ? "[]" : "");
                case "float":
                case "float32":
                case "double":
                case "double32":
                case "single":
                    return "Float" + (isArray ? "[]" : "");
                default:
                    return swExt + type;
                // case "Bool":

            }
            return "";
        }

        public static string Indent(int indents, string line, bool newLine = true)
        {
            string output = "";
            for (int j = 0; j < indents; j++) output += '\t';
            output += line;

            if (newLine) output += Environment.NewLine;

            return output;
        }

        public static bool IsBranchConditionalEQ(Code code)
        {
            return code == Code.Beq || code == Code.Beq_S;
        }

        public static bool IsBranchConditionalLT(Code code)
        {
            return code == Code.Blt || code == Code.Blt_S || code == Code.Blt_Un || code == Code.Blt_Un_S;
        }

        public static bool IsBranchConditionalLE(Code code)
        {
            return code == Code.Ble || code == Code.Ble_S || code == Code.Ble_Un || code == Code.Ble_Un_S;
        }

        public static bool IsBranchConditionalGT(Code code)
        {
            return code == Code.Bgt || code == Code.Bgt_S || code == Code.Bgt_Un || code == Code.Bgt_Un_S;
        }

        public static bool IsBranchConditionalGE(Code code)
        {
            return code == Code.Bge || code == Code.Bge_S || code == Code.Bge_Un || code == Code.Bge_Un_S;
        }


        public static bool IsBranchConditional(Code code)
        {
            return code == Code.Beq || code == Code.Beq_S || code == Code.Bgt || code == Code.Bgt_S || code == Code.Bgt_Un
                    || code == Code.Bgt_Un_S || code == Code.Blt || code == Code.Blt_Un || code == Code.Blt_S || code == Code.Blt_Un_S
                    || code == Code.Ble || code == Code.Ble_Un || code == Code.Ble_S || code == Code.Ble_Un_S || code == Code.Bge
                    || code == Code.Bge_Un || code == Code.Bge_S || code == Code.Bge_Un_S;
        }

        public static bool IsBoxing(Code code)
        {
            return code == Code.Box;
        }

        public static bool IsConverToNumber(Code code)
        {
            return code == Code.Conv_I || code == Code.Conv_I1 || code == Code.Conv_I2 || code == Code.Conv_I4
                    || code == Code.Conv_I8 || code == Code.Conv_Ovf_I || code == Code.Conv_Ovf_I_Un || code == Code.Conv_Ovf_I1
                    || code == Code.Conv_Ovf_I1_Un || code == Code.Conv_Ovf_I2 || code == Code.Conv_Ovf_I2_Un
                    || code == Code.Conv_Ovf_I4 || code == Code.Conv_Ovf_I4_Un || code == Code.Conv_Ovf_I8
                    || code == Code.Conv_Ovf_I8_Un || code == Code.Conv_R_Un || code == Code.Conv_R4 || code == Code.Conv_R8;
        }

        public static object TypeValueConvert(string typeName, object op)
        {
            if (typeName.ToLower().StartsWith("bool") || typeName.ToLower().StartsWith("system.bool"))
            {

                if (op is int || op is float || op is short || op is double || op is long || op is byte) return ((int)Double.Parse(op.ToString()) == 1);
                if (op is bool) return (bool)op;
                if (op is string) return (string)op == "1" || op.ToString().ToLower() == "true";
            }
            if (typeName.ToLower().StartsWith("string") || typeName.ToLower().StartsWith("system.string"))
            {
                if (!op.ToString().Contains("\"")) return "\"" + op.ToString() + "\"";
            }
            else if (op is float || op is decimal || op is double)
            {
                if (op.ToString().Contains(","))
                {
                    return op.ToString().Replace(',', '.');
                }
            }

            return op;
        }

        public static string GetVariableName(Instruction instruction)
        {
            var i = instruction.OpCode.Name;
            if (i.Contains("."))
            {
                var index = i.Split('.').LastOrDefault();
                return "V_" + index;
            }

            // "V_0"
            return "V_" + instruction.Operand;
        }

        public static bool IsLoadArgs(Code code)
        {
            return code == Code.Ldarg || code == Code.Ldarg_0 || code == Code.Ldarg_1 || code == Code.Ldarg_2
                    || code == Code.Ldarg_3 || code == Code.Ldarg_S;
        }

        public static int GetCodeIndex(Code code)
        {
            switch (code)
            {
                case Code.Ldarg_S:
                case Code.Ldarg:
                case Code.Stloc_S:
                case Code.Stloc:
                case Code.Ldc_I4:
                case Code.Ldloc:
                case Code.Ldloc_S:
                    return -1;
                case Code.Ldloc_0:
                case Code.Ldarg_0:
                case Code.Stloc_0:
                case Code.Ldc_I4_0:
                    return 0;
                case Code.Ldloc_1:
                case Code.Ldarg_1:
                case Code.Stloc_1:
                case Code.Ldc_I4_1:
                    return 1;
                case Code.Ldloc_2:
                case Code.Ldarg_2:
                case Code.Stloc_2:
                case Code.Ldc_I4_2:
                    return 2;
                case Code.Ldloc_3:
                case Code.Ldarg_3:
                case Code.Stloc_3:
                case Code.Ldc_I4_3:
                    return 3;
                case Code.Ldc_I4_4:
                    return 4;
                case Code.Ldc_I4_5:
                    return 5;
                case Code.Ldc_I4_6:
                    return 6;
                case Code.Ldc_I4_7:
                    return 7;
                case Code.Ldc_I4_8:
                    return 8;
            }
            return -1;
        }


        public static bool IsCallMethod(Code code)
        {
            return code == Code.Call || code == Code.Calli || code == Code.Callvirt;
        }


        public static bool IsLoadString(Code code)
        {
            return code == Code.Ldstr;
        }


        public static bool IsLoadInteger(Code code)
        {
            return code == Code.Ldc_I4 || code == Code.Ldc_I4_0 || code == Code.Ldc_I4_1 || code == Code.Ldc_I4_2
                    || code == Code.Ldc_I4_3 || code == Code.Ldc_I4_4 || code == Code.Ldc_I4_5 || code == Code.Ldc_I4_6
                    || code == Code.Ldc_I4_7 || code == Code.Ldc_I4_8 || code == Code.Ldc_I4_S || code == Code.Ldc_I8
                    || code == Code.Ldc_R4 || code == Code.Ldc_R8;
        }

        public static bool IsLoadField(Code code)
        {
            return code == Code.Ldfld || code == Code.Ldflda;
        }

        public static bool IsLoadLocalVariable(Code code)
        {
            return code == Code.Ldloc_0 || code == Code.Ldloc || code == Code.Ldloc_1 || code == Code.Ldloc_2
                    || code == Code.Ldloc_3 || code == Code.Ldloc_S;
        }

        public static bool IsLoad(Code code)
        {
            return IsLoadArgs(code) || IsLoadInteger(code) || IsLoadLocalVariable(code) || IsLoadString(code);
        }

        public static bool IsStoreField(Code code)
        {
            return code == Code.Stfld;
        }

        public static bool IsStoreLocalVariable(Code code)
        {
            return code == Code.Stloc || code == Code.Stloc_0 || code == Code.Stloc_1 || code == Code.Stloc_2
                    || code == Code.Stloc_3 || code == Code.Stloc_S;
        }

        public static bool IsGreaterThan(Code code)
        {
            return code == Code.Cgt || code == Code.Cgt_Un;
        }

        public static bool IsLessThan(Code code)
        {
            return code == Code.Clt || code == Code.Clt_Un;
        }

        public static bool IsEqualTo(Code code)
        {
            return code == Code.Ceq;
        }

        public static bool IsMath(Code code)
        {
            return code == Code.Add || code == Code.Sub || code == Code.Div || code == Code.Mul;
        }


        public static bool IsLoadElement(Code code)
        {
            return code == Code.Ldelem_Any || code == Code.Ldelem_I || code == Code.Ldelem_I1 || code == Code.Ldelem_I2
                    || code == Code.Ldelem_I4 || code == Code.Ldelem_I8 || code == Code.Ldelem_R4 || code == Code.Ldelem_R8
                    || code == Code.Ldelem_Ref || code == Code.Ldelem_U1 || code == Code.Ldelem_U2 || code == Code.Ldelem_U4
                    || code == Code.Ldelema;
        }

        public static bool IsStoreElement(Code code)
        {
            return code == Code.Stelem_Any || code == Code.Stelem_I || code == Code.Stelem_I1 || code == Code.Stelem_I2
                    || code == Code.Stelem_I4 || code == Code.Stelem_I8 || code == Code.Stelem_R4 || code == Code.Stelem_R8
                    || code == Code.Stelem_Ref;
        }

        public static bool IsNewInstance(Code code)
        {
            return IsNewArrayInstance(code) || IsNewObjectInstance(code);
        }

        public static bool IsNewArrayInstance(Code code)
        {
            return code == Code.Newarr;
        }

        public static bool IsNewObjectInstance(Code code)
        {
            return code == Code.Newobj;
        }

        public static bool IsLoadNull(Code code)
        {
            return code == Code.Ldnull;
        }

        public class CodeBlock
        {
            public int StartRow { get; set; }

            public int EndRow { get; set; }

            public List<PapyrusLabelReference> UsedLabels = new List<PapyrusLabelReference>();

            public List<PapyrusLabelDefinition> Labels = new List<PapyrusLabelDefinition>();

            public PapyrusLabelDefinition GetLabelDefinition(int row)
            {
                return Labels.FirstOrDefault(r => r.Row == row);
            }
        }

        public static string OptimizeLabels(string input)
        {
            var output = input;
            output = RemoveUnusedLabels(output);
            output = RemoveUnnecessaryLabels(output);
            return output;
        }

        public static string RemoveUnusedLabels(string output)
        {
            var rows = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            var codeBlocks = ParseCodeBlocks(rows);

            Dictionary<int, string> labelsToRemove = new Dictionary<int, string>();

            foreach (var block in codeBlocks)
            {
                foreach (var lbl in block.Labels)
                {
                    bool isCalled = false;
                    foreach (var ulbl in block.UsedLabels)
                    {
                        if (lbl.Name == ulbl.Name)
                        {
                            isCalled = true;
                        }
                    }
                    if (!isCalled)
                    {
                        labelsToRemove.Add(lbl.Row, lbl.Name);
                    }
                }
            }

            var ordered = labelsToRemove.OrderByDescending(i => i.Key).ToArray();

            foreach (var row in ordered)
            {
                rows.RemoveAt(row.Key);
            }

            return String.Join(Environment.NewLine, rows.ToArray());
        }

        public static CodeBlock ParseCodeBlock(string codeBlock)
        {
            var rows = codeBlock.Split('\n');
            return ParseCodeBlocks(rows.ToList()).FirstOrDefault();
        }

        public static List<CodeBlock> ParseCodeBlocks(List<string> rows)
        {
            var codeBlocks = new List<CodeBlock>();
            CodeBlock latestCodeBlock = null;
            int rowI = 0;

            foreach (var row in rows)
            {
                if (row.Replace("\t", "").Trim().StartsWith(".code"))
                {
                    latestCodeBlock = new CodeBlock();
                    latestCodeBlock.StartRow = rowI;
                }
                else if (row.Replace("\t", "").Trim().StartsWith(".endCode"))
                {
                    if (latestCodeBlock != null)
                    {
                        latestCodeBlock.EndRow = rowI;
                        codeBlocks.Add(latestCodeBlock);
                    }
                }
                else if (latestCodeBlock != null)
                {
                    if (row.Replace("\t", "").StartsWith("_") && row.Trim().EndsWith(":"))
                    {
                        latestCodeBlock.Labels.Add(new PapyrusLabelDefinition(rowI, row.Replace("\t", "").Trim()));
                    }
                    else if (row.Replace("\t", "").Contains("_label") /* && !row.Contains(":")*/ && row.ToLower().Contains("jump"))
                    {
                        latestCodeBlock.UsedLabels.Add(new PapyrusLabelReference(row.Substring(row.IndexOf("_label")).Split(' ')[0] + ":", rowI));
                    }
                }
                rowI++;
            }
            return codeBlocks;
        }



        public static string RemoveUnnecessaryLabels(string output)
        {
            var rows = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            var labelReplacements = new List<ObjectReplacementHolder<PapyrusLabelDefinition, PapyrusLabelDefinition, PapyrusLabelReference>>();
            var codeBlocks = ParseCodeBlocks(rows);
            var lastReplacement = new ObjectReplacementHolder<PapyrusLabelDefinition, PapyrusLabelDefinition, PapyrusLabelReference>();
            foreach (var codeBlock in codeBlocks)
            {

                for (int i = 0; i < codeBlock.Labels.Count; i++)
                {
                    var currentLabel = codeBlock.Labels[i];

                    int lastRowIndex = currentLabel.Row;
                    while (i + 1 < codeBlock.Labels.Count)
                    {
                        if (lastReplacement == null)
                        {
                            lastReplacement = new ObjectReplacementHolder<PapyrusLabelDefinition, PapyrusLabelDefinition, PapyrusLabelReference>();
                        }

                        var label = codeBlock.GetLabelDefinition(lastRowIndex + 1);
                        if (label != null)
                        {
                            if (lastReplacement.Replacement == null) lastReplacement.Replacement = currentLabel;

                            lastReplacement.ToReplace.Add(label);

                            var usedAreas = codeBlock.UsedLabels.Where(b => b.Name == label.Name);
                            if (usedAreas.Any())
                            {
                                lastReplacement.ToReplaceSecondary.AddRange(usedAreas.ToArray());
                            }


                            lastRowIndex = label.Row;
                            // We have a previous label one row behind us.

                        }
                        else
                        {

                            break;
                        }
                        i++;
                    }
                    if (lastReplacement != null && lastReplacement.ToReplace.Count > 0)
                    {
                        labelReplacements.Add(lastReplacement);
                        lastReplacement = null;
                    }

                }
            }
            var rowsToRemove = new List<int>();
            foreach (var replacer in labelReplacements)
            {

                foreach (var old in replacer.ToReplace)
                {
                    rows[old.Row] = rows[old.Row].Replace(old.Name, replacer.Replacement.Name);
                    rowsToRemove.Add(old.Row);
                }
                foreach (var old in replacer.ToReplaceSecondary)
                {
                    rows[old.RowReference] = rows[old.RowReference].Replace(
                        old.Name.Remove(old.Name.Length - 1), replacer.Replacement.Name.Remove(replacer.Replacement.Name.Length - 1));
                }
            }

            foreach (var r in rowsToRemove.OrderByDescending(v => v))
            {
                rows.RemoveAt(r);
            }

            return String.Join(Environment.NewLine, rows.ToArray());
        }

        public static string InjectTempVariables(string output, int indentDepth, List<PapyrusVariableReference> TempVariables)
        {
            var rows = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            // foreach(var )
            int insertIndex = Array.IndexOf(rows.ToArray(), rows.FirstOrDefault(r => r.ToLower().Contains(".endlocaltable")));


            foreach (var variable in TempVariables)
            {
                rows.Insert(insertIndex, Indent(indentDepth, variable.Definition, false));
            }


            return String.Join(Environment.NewLine, rows.ToArray());
        }

        public static int GetStackPopCount(StackBehaviour stackBehaviour)
        {
            switch (stackBehaviour)
            {
                case StackBehaviour.Varpop:
                case StackBehaviour.Pop0:
                case StackBehaviour.Popi:
                case StackBehaviour.Pop1:
                case StackBehaviour.Popref:
                    return 1;
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popref_popi:
                    return 2;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;
                case StackBehaviour.PopAll:
                    return 9999;
            }
            return 0;
        }


        public static string GetPapyrusMathOp(Code code)
        {
            switch (code)
            {
                case Code.Add_Ovf:
                case Code.Add_Ovf_Un:
                case Code.Add:
                    return "IAdd";
                case Code.Sub:
                case Code.Sub_Ovf:
                case Code.Sub_Ovf_Un:
                    return "ISubtract";
                case Code.Div_Un:
                case Code.Div:
                    return "IDivide";
                case Code.Mul:
                case Code.Mul_Ovf:
                case Code.Mul_Ovf_Un:
                    return "IMultiply";
                default:
                    return "IAdd";
            }
        }


        public static bool IsLoadLength(Code code)
        {
            //throw new NotImplementedException();
            return code == Code.Ldlen;
        }


        public static string GetPropertyName(string p)
        {
            string outName = p;
            if (p.StartsWith("::"))
            {
                outName = outName.Substring(2);
            }
            return "p" + outName;
        }
    }
}
