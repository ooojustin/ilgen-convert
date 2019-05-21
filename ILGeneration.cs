using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ilgen_convert {

    public static class ILGeneration {

        public static void CreateDynamicMethod(this ILProcessor processor, string name, TypeReference returnType, Collection<ParameterDefinition> parameters) {

            // determine the name of dynamic method (param #1)
            processor.Emit(OpCodes.Ldstr, name);

            // determine the return type of the method (param #2)
            processor.Emit(OpCodes.Ldtoken, returnType);
            processor.Emit(OpCodes.Call, Program.MethodReferences["GetTypeFromHandle"]);

            // create a new array to hold parameter types (param #3)
            processor.EmitTypeArray(parameters);

            // create an instance of DynamicMethod with the parameters on the stack (string, Type, Type[])
            processor.Emit(OpCodes.Newobj, Program.MethodReferences["DynamicMethodConstructor"]);

        }

        /// new Type[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(System.Reflection.Binder), typeof(Type[]), typeof(System.Reflection.ParameterModifier[]) }
        public static void EmitMethodGetter(this ILProcessor processor, MethodDefinition method) {

            // get the parent type and store it on the stack
            processor.EmitType(method.DeclaringType);

            if (method.IsConstructor) {

                processor.EmitTypeArray(method.Parameters);

                processor.Emit(OpCodes.Callvirt, Program.MethodReferences["GetConstructorInfoTypes"]);

            }

            // make sure the method is public
            method.IsPrivate = false;
            method.IsPublic = true;

            // the method name (param #1)
            processor.Emit(OpCodes.Ldstr, method.Name);

            // the binding flags (param #2)
            processor.Emit(OpCodes.Ldc_I4, Utils.GetBindingFlags(method));

            // the binder (param #3)
            processor.Emit(OpCodes.Ldnull);

            // the parameter types (param #4)
            processor.EmitTypeArray(method.Parameters);

            // the parameter modifiers (param $5)
            processor.Emit(OpCodes.Ldnull);

            // call GetMethodInfo function, leaving the returned value on the eval stack
            processor.Emit(OpCodes.Callvirt, Program.MethodReferences["GetMethodInfo"]);

        }

        public static void EmitMethodGetter(this ILProcessor processor, MethodReference method) {

            processor.EmitType(method.DeclaringType);

            processor.Emit(OpCodes.Ldstr, method.Name);

            processor.EmitTypeArray(method.Parameters);

            processor.Emit(OpCodes.Call, Program.MethodReferences["GetMethodInfoTypes"]);

        }

        public static void EmitFieldGetter(this ILProcessor processor, FieldDefinition field) {

            // make sure the field is public
            field.IsPrivate = false;
            field.IsPublic = true;

            // get the parent type and store it on the stack
            processor.EmitType(field.DeclaringType);

            // store the parameter name on the stack (param #1)
            processor.Emit(OpCodes.Ldstr, field.Name);

            // store the binding fields on the stack (param #2)
            processor.Emit(OpCodes.Ldc_I4, Utils.GetBindingFlags(field));

            // call the function to store FieldInfo object on  the stack (string, BindingFlags)
            processor.Emit(OpCodes.Callvirt, Program.MethodReferences["GetFieldInfo"]);

        }

        public static void EmitTypeArray(this ILProcessor processor, Collection<ParameterDefinition> parameters) {

            // determine length of array
            processor.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(parameters.Count));

            // create the array
            processor.Emit(OpCodes.Newarr, Program.TypeReferences["Type"]);

            // iterate through parameters in collection
            for (int pI = 0; pI < parameters.Count; pI++) {

                // load array object onto eval stack
                processor.Emit(OpCodes.Dup);

                // load the index onto the stack
                processor.Emit(OpCodes.Ldc_I4, pI);

                // load the type onto the stack
                processor.EmitType(parameters[pI].ParameterType);

                // move the 'type' into array and index pI
                processor.Emit(OpCodes.Stelem_Ref);

            }

        }

        public static void EmitType(this ILProcessor processor, TypeReference reference) {
            processor.Emit(OpCodes.Ldtoken, reference);
            processor.Emit(OpCodes.Call, Program.MethodReferences["GetTypeFromHandle"]);
        }

        public static void EmitMarkLabel(this ILProcessor processor, VariableDefinition label) {
            processor.Emit(OpCodes.Ldloc, label);
            processor.Emit(OpCodes.Callvirt, Program.MethodReferences["MarkLabel"]);
        }

    }

}
