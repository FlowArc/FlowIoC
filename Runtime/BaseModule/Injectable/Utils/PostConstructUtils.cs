// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using FlowIoC.BaseModule.Injectable.Attributes;
//
// namespace FlowIoC.BaseModule.Injectable.Utils
// {
//     internal static class PostConstructUtils
//     {
//         public static void ExecutePostConstructMethod(object target)
//         {
//             if (target == null) return;
//
//             Type type = target.GetType();
//             MethodInfo[] allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//             List<MethodInfo> postConstructMethods = null;
//
//             foreach (MethodInfo methodInfo in allMethods)
//             {
//                 if (methodInfo.GetCustomAttributes(typeof(PostConstructAttribute), true).Length != 0)
//                 {
//                     postConstructMethods ??= new List<MethodInfo>();
//                     postConstructMethods.Add(methodInfo);
//                 }
//             }
//
//             if (postConstructMethods != null)
//             {
//                 foreach (MethodInfo deconstructMethod in postConstructMethods)
//                 {
//                     deconstructMethod.Invoke(target, null);
//                 }
//             }
//         }
//     }
// }