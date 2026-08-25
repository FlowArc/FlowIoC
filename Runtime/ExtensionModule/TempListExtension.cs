using System.Collections.Generic;

namespace FlowIoC.ExtensionModule
{
    public static class TempListExtension
    {
        public static List<TTo> GetOrAssignIfNullFrom<TTo,TFrom>(this List<TTo> toList, List<TFrom> fromList) where TFrom : class where TTo : class
        {
            if(toList == null)
            {
                toList = new List<TTo>();
            
                foreach(TFrom fromValue in fromList)
                {
                    TTo toValue = fromValue as TTo;
                    toList.Add(toValue);
                }
            }
            
            return toList;
        }
    }
}