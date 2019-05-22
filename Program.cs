using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ilgen_convert {

    class Program {

        public static ModuleDefinition Module = null;

        public static Dictionary<string, MethodReference> MethodReferences = new Dictionary<string, MethodReference>();
        public static Dictionary<string, TypeReference> TypeReferences = new Dictionary<string, TypeReference>();

        static void Main(string[] args) {

            string assembly = "test.exe";
            Module = ModuleDefinition.ReadModule(assembly);

            MethodReferences.Add("FinallyBlock", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("BeginFinallyBlock", new Type[0])));
            MethodReferences.Add("CatchBlock", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("BeginCatchBlock", new Type[] { typeof(Type) })));
            MethodReferences.Add("TryEnd", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("EndExceptionBlock", new Type[0])));
            MethodReferences.Add("TryStart", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("BeginExceptionBlock", new Type[0])));
            MethodReferences.Add("DeclareLocal", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("DeclareLocal", new Type[] { typeof(Type) })));
            MethodReferences.Add("MarkLabel", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("MarkLabel", new Type[] { typeof(System.Reflection.Emit.Label) })));
            MethodReferences.Add("DefineLabel", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator).GetMethod("DefineLabel")));
            MethodReferences.Add("GetMethodInfo", Module.ImportReference(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(System.Reflection.Binder), typeof(Type[]), typeof(System.Reflection.ParameterModifier[]) })));
            MethodReferences.Add("GetMethodInfoTypes", Module.ImportReference(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(Type[]) })));
            MethodReferences.Add("GetTypeFromHandle", Module.ImportReference(typeof(System.Type).GetMethod("GetTypeFromHandle")));
            MethodReferences.Add("DynamicMethodConstructor", Module.ImportReference(typeof(System.Reflection.Emit.DynamicMethod).GetConstructor(new Type[] { typeof(string), typeof(Type), typeof(Type[]) })));
            MethodReferences.Add("GetILGenerator", Module.ImportReference(typeof(System.Reflection.Emit.DynamicMethod).GetMethod("GetILGenerator", new Type[] { })));
            MethodReferences.Add("GetFieldInfo", Module.ImportReference(typeof(Type).GetMethod("GetField", new Type[] { typeof(string), typeof(System.Reflection.BindingFlags) })));
            MethodReferences.Add("GetConstructorInfoTypes", Module.ImportReference(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) })));
            MethodReferences.Add("Invoker", Module.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) })));

            TypeReferences.Add("Type", Module.ImportReference(typeof(Type)));
            TypeReferences.Add("Label", Module.ImportReference(typeof(System.Reflection.Emit.Label)));
            TypeReferences.Add("LocalBuilder", Module.ImportReference(typeof(System.Reflection.Emit.LocalBuilder)));
            TypeReferences.Add("ILGenerator", Module.ImportReference(typeof(System.Reflection.Emit.ILGenerator)));


            foreach (TypeDefinition type in Module.Types) {

                foreach (MethodDefinition method in type.Methods) {

                    if (method.Name == ".ctor" || method.Name == ".cctor" || !method.HasBody)
                        continue;

                    Console.WriteLine(" ===== " + method.FullName + " ===== ");
                    method.Body = GenerateBody(method);
                    Console.WriteLine("\n");

                    // make sure the class is public so it can be accessed when the DynamicMethod is invoked
                    type.IsNotPublic = false;
                    type.IsPublic = true;

                }

            }

            Console.ReadKey();
            Module.Write("test_dynamic.exe");

        }

        private static MethodBody GenerateBody(MethodDefinition method) {

            MethodBody body = new MethodBody(method);
            ILProcessor processor = body.GetILProcessor();

            Dictionary<Instruction, VariableDefinition> Branches = new Dictionary<Instruction, VariableDefinition>();
            Dictionary<int, VariableDefinition> Locals = new Dictionary<int, VariableDefinition>();

            // create an instance of DynamicMethod
            processor.CreateDynamicMethod("", method.ReturnType, method.Parameters);

            // establish a variable to store ilgenerator
            VariableDefinition ilgenerator = new VariableDefinition(TypeReferences["ILGenerator"]);
            processor.Body.Variables.Add(ilgenerator);

            // generate an ILGenerator object from the DynamicMethod
            processor.Emit(OpCodes.Dup);
            processor.Emit(OpCodes.Callvirt, MethodReferences["GetILGenerator"]);

            // store ilgenerator object into local variable
            processor.Emit(OpCodes.Stloc, ilgenerator);

            // pre-emission phase (runs after DynamicMethod and ILGenerator are instantiated)
            foreach (Instruction instruction in method.Body.Instructions) {

                // output instructions & operand types to console
                Console.Write(instruction.Offset + ": " + instruction.OpCode.Name);
                Console.Write(instruction.Operand == null ? "\n" : "        -> " + instruction.Operand.GetType() + "\n");

                // define branch variables ('Label' objects)
                if (instruction.Operand is Instruction) {

                    Instruction target = instruction.Operand as Instruction;

                    if (Branches.ContainsKey(target))
                        continue;

                    VariableDefinition label = new VariableDefinition(TypeReferences["Label"]);
                    processor.Body.Variables.Add(label);

                    processor.Emit(OpCodes.Ldloc, ilgenerator);
                    processor.Emit(OpCodes.Callvirt, MethodReferences["DefineLabel"]);
                    processor.Emit(OpCodes.Stloc, label);

                    Branches.Add(target, label);

                }

            }

            // define local variables
            for (int vI = 0; vI < method.Body.Variables.Count; vI++) {

                VariableDefinition local = new VariableDefinition(TypeReferences["LocalBuilder"]);
                processor.Body.Variables.Add(local);

                TypeReference variableType = method.Body.Variables[vI].VariableType;

                processor.Emit(OpCodes.Ldloc, ilgenerator);
                processor.EmitType(variableType);
                processor.Emit(OpCodes.Callvirt, MethodReferences["DeclareLocal"]);
                processor.Emit(OpCodes.Stloc, local);

                Locals.Add(vI, local);

            }


            // iterate through instructions, writer ILGenerator.Emit calls
            for (int iI = 0; iI < method.Body.Instructions.Count; iI++) {

                // the current instruction
                Instruction instruction = method.Body.Instructions[iI];

                // mark a label for this instruction, if we have a branch going here
                if (Branches.ContainsKey(instruction)) {
                    processor.Emit(OpCodes.Ldloc, ilgenerator);
                    processor.EmitMarkLabel(Branches[instruction]);
                }

                // load ilgenerator object into memory to make Emit call from it
                processor.Emit(OpCodes.Ldloc, ilgenerator);

                // determine index of stloc/ldloc call based on OpCode
                int stlocIndex = instruction.GetStlocIndex();
                int ldlocIndex = instruction.GetLdlocIndex();

                // modify stloc/ldloc implementation to use LocalBuilder
                if (stlocIndex > -1 || ldlocIndex > -1) {

                    // new determine new OpCode & local variable index
                    bool isStloc = stlocIndex > -1;
                    instruction.OpCode = isStloc ? OpCodes.Stloc : OpCodes.Ldloc;
                    int localIndex = isStloc ? stlocIndex : ldlocIndex;

                    // load OpCode (param #1) and LocalBuilder object (param #2) onto eval stack
                    // make ILGenerator.Emit call with these 2 params to fix local variables in ILGenerator methods
                    processor.Emit(OpCodes.Ldsfld, Utils.GetReflectedOpCode(instruction));
                    processor.Emit(OpCodes.Ldloc, Locals[localIndex]);
                    processor.Emit(OpCodes.Callvirt, Utils.GetILGeneratorEmitter(typeof(System.Reflection.Emit.LocalBuilder)));
                    continue;

                }

                // load the OpCode to be emitted onto the eval stack (param #1)
                processor.Emit(OpCodes.Ldsfld, Utils.GetReflectedOpCode(instruction));

                // type of ILGenerator.Emit function to invoke.
                // if null, Emit will be invoked without an operand.
                // all operands must be handled in the following if/else blocks
                Type EmitType = null;

                // handle operands (note: each operand needs to be handled differently)
                // this will be used in the ILGenerator.Emit call (param #2)
                if (instruction.Operand != null) {
                    if (instruction.Operand is FieldDefinition) {
                        FieldDefinition fieldDefinition = instruction.Operand as FieldDefinition;
                        processor.EmitFieldGetter(fieldDefinition);
                        EmitType = typeof(System.Reflection.FieldInfo);
                    } else if (instruction.Operand is MethodDefinition) {
                        MethodDefinition methodDefinition = instruction.Operand as MethodDefinition;
                        processor.EmitMethodGetter(methodDefinition);
                        EmitType = methodDefinition.IsConstructor ? typeof(System.Reflection.ConstructorInfo) : typeof(System.Reflection.MethodInfo);
                    } else if (instruction.Operand is MethodReference) {
                        MethodReference methodReference = instruction.Operand as MethodReference;
                        processor.EmitMethodGetter(methodReference);
                        EmitType = typeof(System.Reflection.MethodInfo);
                    } else if (instruction.Operand.GetType() == typeof(sbyte)) {
                        sbyte value = Convert.ToSByte(instruction.Operand);
                        processor.Emit(OpCodes.Ldc_I4_S, value);
                        EmitType = typeof(sbyte);
                    } else if (instruction.Operand.GetType() == typeof(string)) {
                        string value = Convert.ToString(instruction.Operand);
                        processor.Emit(OpCodes.Ldstr, value);
                        EmitType = typeof(string);
                    } else if (instruction.Operand.GetType() == typeof(int)) {
                        int value = Convert.ToInt32(instruction.Operand);
                        processor.Emit(OpCodes.Ldc_I4, value);
                        EmitType = typeof(int);
                    } else if (instruction.Operand is Instruction) {
                        Instruction targetInstruction = instruction.Operand as Instruction;
                        processor.Emit(OpCodes.Ldloc, Branches[targetInstruction]);
                        EmitType = typeof(System.Reflection.Emit.Label);
                    } else {
                        Console.WriteLine("UNHANDLED OPERAND: opcode = " + instruction.OpCode.Name + ", type = " + instruction.Operand.GetType());
                    }

                }

                // call the ILGenerator.Emit func
                // if EmitType is null, second parameter (operand) is ignored
                processor.Emit(OpCodes.Callvirt, Utils.GetILGeneratorEmitter(EmitType));

            }

            // object parameter in DynamicMethod.Invoke, should always be null (param #1)
            processor.Emit(OpCodes.Ldnull);

            // create an array of objects to hold parameters to send to DynamicMethod.Invoke (param #2)
            processor.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(method.Parameters.Count));
            processor.Emit(OpCodes.Newarr, Module.TypeSystem.Object);

            // load parameters into the created array
            for (int pI = 0; pI < method.Parameters.Count; pI++) {
                ParameterDefinition parameter = method.Parameters[pI];
                processor.Emit(OpCodes.Dup);
                processor.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(pI));
                processor.Emit(OpCodes.Ldarg_S, parameter);
                processor.Emit(OpCodes.Box, parameter.ParameterType);
                processor.Emit(OpCodes.Stelem_Ref);
            }

            // call the invoker
            processor.Emit(OpCodes.Callvirt, MethodReferences["Invoker"]);

            // cast the returned object to the return type
            if (method.ReturnType != Module.TypeSystem.Void)
                processor.Emit(OpCodes.Unbox_Any, method.ReturnType);
            else
                processor.Emit(OpCodes.Pop);

            // return the remaining value on the stack (result of dynamic method)
            processor.Emit(OpCodes.Ret);

            return body;

        }

    }

}
