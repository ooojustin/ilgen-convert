using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ilgen_convert {

    public static class Utils {

        public static int GetBindingFlags(FieldDefinition field) {

            int flags = 0;

            if ((field.Attributes & FieldAttributes.Static) != 0)
                flags |= (int)System.Reflection.BindingFlags.Static;
            else
                flags |= (int)System.Reflection.BindingFlags.Instance;

            if ((field.Attributes & FieldAttributes.Public) != 0)
                flags |= (int)System.Reflection.BindingFlags.Public;
            else
                flags |= (int)System.Reflection.BindingFlags.NonPublic;

            return flags;

        }

        public static int GetBindingFlags(MethodDefinition method) {

            int flags = 0;

            if ((method.Attributes & MethodAttributes.Static) != 0)
                flags |= (int)System.Reflection.BindingFlags.Static;
            else
                flags |= (int)System.Reflection.BindingFlags.Instance;

            if ((method.Attributes & MethodAttributes.Public) != 0)
                flags |= (int)System.Reflection.BindingFlags.Public;
            else
                flags |= (int)System.Reflection.BindingFlags.NonPublic;

            return flags;

        }

        public static FieldReference GetReflectedOpCode(Instruction instruction) {

            // get the op code name
            string opCodeName = instruction.OpCode.Code.ToString();

            // repair it, if needed
            if (opCodeName == "Ldelem_Any")
                opCodeName = "Ldelem"; // System.Reflection.Emit.OpCodes.Ldelem
            else if (opCodeName == "Stelem_Any")
                opCodeName = "Stelem"; // System.Reflection.Emit.OpCodes.Stelem

            // get FieldInfo object
            System.Reflection.FieldInfo opCodeFieldInfo = typeof(System.Reflection.Emit.OpCodes).GetField(opCodeName);

            // return a reference to the field
            return Program.Module.ImportReference(typeof(System.Reflection.Emit.OpCodes).GetField(opCodeName));

        }

        public static MethodReference GetILGeneratorEmitter(Type t = null) {
            if (t == null)
                return Program.Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("Emit", new Type[] { typeof(System.Reflection.Emit.OpCode) }));
            else
                return Program.Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("Emit", new Type[] { typeof(System.Reflection.Emit.OpCode), t }));
        }

        public static int GetStlocIndex(this Instruction instruction) {
            OpCode[] list = {
                OpCodes.Stloc_0, // 0
                OpCodes.Stloc_1, // 1
                OpCodes.Stloc_2, // 2
                OpCodes.Stloc_3, // 3
                /* ---- OPERAND PROVIDERS --- */
                OpCodes.Stloc_S, // 4
                OpCodes.Stloc // 5
            };
            int index = Array.IndexOf(list, instruction.OpCode);
            if (index > 3)
                return Convert.ToInt32(instruction.Operand);
            else
                return index;
        }

        public static int GetLdlocIndex(this Instruction instruction) {
            // note: exclude ldloca & ldloca.s because they load addresses
            OpCode[] list = {
                OpCodes.Ldloc_0, // 0
                OpCodes.Ldloc_1, // 1
                OpCodes.Ldloc_2, // 2
                OpCodes.Ldloc_3, // 3
                /* ---- OPERAND PROVIDERS --- */
                OpCodes.Ldloc_S, // 4
                OpCodes.Ldloc // 5
            };
            int index = Array.IndexOf(list, instruction.OpCode);
            return index > 3 ? Convert.ToInt32(instruction.Operand) : index;
        }

    }

}
