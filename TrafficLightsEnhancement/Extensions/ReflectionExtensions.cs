

namespace C2VM.TrafficLightsEnhancement.Extensions
{
    
    using System.Reflection;
  

    
    
    
    public static class ReflectionExtensions
    {
        
        
        
        public static readonly BindingFlags AllFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty;

        
        
        
        
        
        
        
        public static object GetMemberValue(this object obj, string memberName)
        {
            var memInf = GetMemberInfo(obj, memberName);
            if (memInf == null)
            {
                Mod.m_Log.Error(new System.Exception("memberName"), $"{nameof(ReflectionExtensions)} {nameof(GetMemberInfo)} Couldn't find member name! ");
            }

            if (memInf is PropertyInfo)
            {
                return memInf.As<PropertyInfo>().GetValue(obj, null);
            }

            if (memInf is FieldInfo)
            {
                return memInf.As<FieldInfo>().GetValue(obj);
            }

            throw new System.Exception();
        }

        
        
        
        
        
        
        
        
        public static object SetMemberValue(this object obj, string memberName, object newValue)
        {
            var memInf = GetMemberInfo(obj, memberName);
            if (memInf == null)
            {
                Mod.m_Log.Error(new System.Exception("memberName"), $"{nameof(ReflectionExtensions)} {nameof(GetMemberInfo)} Couldn't find member name! ");
            }

            var oldValue = obj.GetMemberValue(memberName);
            if (memInf is PropertyInfo)
            {
                memInf.As<PropertyInfo>().SetValue(obj, newValue, null);
            }
            else if (memInf is FieldInfo)
            {
                memInf.As<FieldInfo>().SetValue(obj, newValue);
            }
            else
            {
                throw new System.Exception();
            }

            return oldValue;
        }

        
        
        
        
        
        
        private static MemberInfo GetMemberInfo(object obj, string memberName)
        {
            var prps = new System.Collections.Generic.List<PropertyInfo>
        {
            obj.GetType().GetProperty(
                memberName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy),
        };
            prps = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(prps, i => i is not null));
            if (prps.Count != 0)
            {
                return prps[0];
            }

            var flds = new System.Collections.Generic.List<FieldInfo>
        {
            obj.GetType().GetField(
                memberName,
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy),
        };
            flds = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(flds, i => i is not null));
            if (flds.Count != 0)
            {
                return flds[0];
            }

            return null;
        }

        [System.Diagnostics.DebuggerHidden]
        private static T As<T>(this object obj)
        {
            return (T)obj;
        }
    }
}