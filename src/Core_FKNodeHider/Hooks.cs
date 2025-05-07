using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace FKNodeHider
{
    [HarmonyPatch(typeof(BoneLineCtrl), nameof(BoneLineCtrl.Draw), typeof(OCIChar))]
    public static class BoneLineTranspiler
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            FKNodeHider.Logger.LogInfo("Hooks.Transpiler");
            CodeMatcher cm = new CodeMatcher(instructions, generator);

            while (cm.Remaining > 0)
            {
                cm.MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(OICharInfo), nameof(OICharInfo.activeFK))));
                if (cm.Remaining < 1) break;
                cm.Advance(1); // next line$ loads the integer that corresponds to the fkCategory
                CodeInstruction loadInt = cm.Instruction.Clone();
                cm.Advance(2); // go to brfalse.s
                CodeInstruction jump = cm.Instruction.Clone(); // remember where this would jump to
                cm.Advance(1);
                cm.InsertAndAdvance(new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    loadInt,
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FKNodeHider), nameof(FKNodeHider.ShouldDrawLine))),
                    jump
                });
            }

            List<CodeInstruction> codeInstructions = cm.Instructions();
            return codeInstructions;
        }
    }
    
    [HarmonyPatch]
    public static class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MPCharCtrl.FKInfo), nameof(MPCharCtrl.FKInfo.UpdateInfo))]
        static void UpdateInfoPostfix(OCIChar _char)
        {
            FKNodeHider.UpdateInfo(_char);
        }
    }
}