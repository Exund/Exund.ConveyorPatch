using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace Exund.ConveyorPatch
{
    public class ConveyorPatchMod : ModBase
    {
        private static readonly FieldInfo m_Output = AccessTools.Field(typeof(ModuleItemConsume), "m_Output");
        private static readonly FieldInfo m_Holder = AccessTools.Field(typeof(ModuleItemConveyor), "m_Holder");

        internal const string HarmonyID = "Exund.ConveyorPatch";

        internal static Harmony harmony = new Harmony(HarmonyID);

        public static void Load()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Init()
        {
            Load();
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }

        public static void ConveyorPatch(ModuleItemConveyor fromConveyor)
        {
            var holder = (ModuleItemHolder)m_Holder.GetValue(fromConveyor);
            foreach (ModuleItemHolder.Stack stack in holder.SingleStack.ConnectedStacks)
            {
                ModuleItemConsume consume = stack.myHolder?.GetComponent<ModuleItemConsume>();
                if (consume)
                {
                    ModuleItemHolder.StackHandle stackHandle = (ModuleItemHolder.StackHandle)m_Output.GetValue(consume);
                    if (stackHandle?.stack == stack && !stack.IsEmpty && !stack.ReceivedThisHeartbeat)
                    {
                        holder.SingleStack.TryTakeOnHeartbeat(stack.FirstItem);
                        break;
                    }
                }
            }
        }
    }

    internal static class Patches
    {
        [HarmonyPatch(typeof(global::ModuleItemConveyor), "OnCycle")]
        private static class OnCycle
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var i = codes.FindIndex(ci => ci.opcode == OpCodes.Ldloc_S && (ci.operand as LocalBuilder).LocalType == typeof(TechHolders.OperationResult));
                codes.Insert(i, new CodeInstruction(OpCodes.Call, typeof(ConveyorPatchMod).GetMethod("ConveyorPatch")));
                codes.Insert(i, new CodeInstruction(OpCodes.Ldloc_S, 1));
                return codes;
            }
		}
    }
}
