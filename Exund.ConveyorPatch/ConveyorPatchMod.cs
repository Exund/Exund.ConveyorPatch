using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Harmony;

namespace Exund.ConveyorPatch
{
    public class ConveyorPatchMod
    {
        static BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static FieldInfo m_Output = typeof(ModuleItemConsume).GetField("m_Output", flags);
        static FieldInfo m_Holder = typeof(ModuleItemConveyor).GetField("m_Holder", flags);

        public static void Load()
        {
            var harmony = HarmonyInstance.Create("Exund.ConveyorPatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void ConveyorPatch(ModuleItemConveyor fromConveyor)
        {
            var holder = (ModuleItemHolder)m_Holder.GetValue(fromConveyor);
            foreach (ModuleItemHolder.Stack stack in holder.SingleStack.ConnectedStacks)
            {
                ModuleItemConsume consume = stack.myHolder?.GetComponent<ModuleItemConsume>();
                if (consume)
                {
                    //Console.WriteLine(consume.block.name);
                    ModuleItemHolder.StackHandle stackHandle = (ModuleItemHolder.StackHandle)m_Output.GetValue(consume);
                    if (stackHandle?.stack == stack && !stack.IsEmpty && !stack.ReceivedThisHeartbeat)
                    {
                        //Console.WriteLine(stack.basePos);
                        holder.SingleStack.TryTakeOnHeartbeat(stack.FirstItem);
                        break;
                    }
                }
            }
        }
    }

    static class Patches
    {
        [HarmonyPatch(typeof(ModuleItemConveyor), "OnCycle")]
        private static class OnCycle
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var i = codes.FindLastIndex(ci => ci.opcode == OpCodes.Brfalse_S) + 1;
                codes.Insert(i, new CodeInstruction(OpCodes.Call, typeof(ConveyorPatchMod).GetMethod("ConveyorPatch")));
                codes.Insert(i, new CodeInstruction(OpCodes.Ldloc_S, 1));
                return codes;
            }
        }
    }
}
