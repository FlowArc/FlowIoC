// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using FlowIoC.BaseModule.Injectable.Attributes;
//
// namespace FlowIoC.BaseModule.Injectable.Utils
// {
//     internal static class DeconstructUtils
//     {
//         public static void ExecuteDeconstructMethod(object target)
//         {
//             if (target == null) return;
//
//             Type type = target.GetType();
//             MethodInfo[] allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//             List<MethodInfo> deconstructMethods = null;
//
//             foreach (MethodInfo methodInfo in allMethods)
//             {
//                 if (methodInfo.GetCustomAttributes(typeof(DeconstructAttribute), true).Length != 0)
//                 {
//                     deconstructMethods ??= new List<MethodInfo>();
//                     deconstructMethods.Add(methodInfo);
//                 }
//             }
//
//             if (deconstructMethods != null)
//             {
//                 foreach (MethodInfo deconstructMethod in deconstructMethods)
//                 {
//                     deconstructMethod.Invoke(target, null);
//                 }
//             }
//         }
//     }
// }