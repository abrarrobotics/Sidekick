using UnityEngine;
using System.Collections;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using System;
using Object = UnityEngine.Object;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    /// <summary>
    /// Sets a field or property's value on the Unity object that instanceID maps to
    /// </summary>
    public class SetVariableRequest : BaseRequest
    {
        Guid guid;
        WrappedVariable wrappedVariable;

        public SetVariableRequest(Guid guid, WrappedVariable wrappedVariable)
        {
            this.guid = guid;
            this.wrappedVariable = wrappedVariable;
        }

        public SetVariableRequest(BinaryReader br)
        {
            this.guid = new Guid(br.ReadString());
            this.wrappedVariable = new WrappedVariable(br);
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            bw.Write(guid.ToString());
            wrappedVariable.Write(bw);
        }

        public override BaseResponse GenerateResponse()
        {
            object targetObject = ObjectMap.GetObjectFromGUID(guid);

            if (targetObject != null)
            {
                BindingFlags bindingFlags = GetGameObjectRequest.BINDING_FLAGS;

                if (targetObject.GetType() == typeof(GameObject) && wrappedVariable.VariableName == "name") // Special handling for GameObject.name to always be included
                {
                    bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                }

                FieldInfo fieldInfo = targetObject.GetType().GetField(wrappedVariable.VariableName, bindingFlags);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(targetObject, wrappedVariable.ValueNative);
                }
                else
                {
                    PropertyInfo propertyInfo = targetObject.GetType().GetProperty(wrappedVariable.VariableName, bindingFlags);
                    MethodInfo setMethod = propertyInfo.GetSetMethod();

                    setMethod.Invoke(targetObject, new object[] { wrappedVariable.ValueNative });
                }
            }
            else
            {
                throw new System.NullReferenceException();
            }

            return new SetVariableResponse();
        }
    }
}