using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sabresaurus.Sidekick.Requests
{
    //[Flags]
    //public enum InfoFlags : byte
    //{
    //    Fields = 1,
    //    Properties = 2,
    //    Methods = 4,
    //}

    /// <summary>
    /// Gets reflected information about components on a game object specified by path. Flags specify what information to include.
    /// </summary>
    public class GetObjectRequest : BaseRequest
    {
        string gameObjectPath;
        InfoFlags flags;

        public GetObjectRequest(string gameObjectPath, InfoFlags flags)
        {
            this.gameObjectPath = gameObjectPath;
            this.flags = flags;
        }

        public GetObjectRequest(BinaryReader br)
        {
            this.gameObjectPath = br.ReadString();
            this.flags = (InfoFlags)br.ReadInt32();
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            bw.Write(gameObjectPath);
            bw.Write((int)flags);
        }

        public override BaseResponse GenerateResponse()
        {
            GetGameObjectResponse getGOResponse = new GetGameObjectResponse();

            Transform foundTransform = TransformHelper.GetFromPath(gameObjectPath);
            getGOResponse.GameObjectName = foundTransform.name;

            List<Object> components = new List<Object>(foundTransform.GetComponents<Component>());
            // Not technically a component, but include the GameObject
            components.Insert(0, foundTransform.gameObject);
            getGOResponse.Components = new List<ComponentDescription>(components.Count);
            foreach (Object component in components)
            {
                //Guid guid = ObjectMap.AddOrGetObject(component);
                ObjectMap.AddOrGetObject(component);

                ComponentDescription description = new ComponentDescription(component);
                Type componentType = component.GetType();

                if ((flags & InfoFlags.Fields) == InfoFlags.Fields)
                {
                    FieldInfo[] fieldInfos = componentType.GetFields(GetGameObjectRequest.BINDING_FLAGS);
                    foreach (FieldInfo fieldInfo in fieldInfos)
                    {
                        if (TypeUtility.IsBackingField(fieldInfo, componentType))
                        {
                            // Skip backing fields for auto-implemented properties
                            continue;
                        }

                        object objectValue = fieldInfo.GetValue(component);

                        WrappedVariable wrappedVariable = new WrappedVariable(fieldInfo, objectValue);
                        description.Fields.Add(wrappedVariable);
                    }
                }

                if ((flags & InfoFlags.Properties) == InfoFlags.Properties)
                {
                    PropertyInfo[] properties = componentType.GetProperties(GetGameObjectRequest.BINDING_FLAGS);
                    foreach (PropertyInfo property in properties)
                    {
                        Type declaringType = property.DeclaringType;
                        if (declaringType == typeof(Component)
                            || declaringType == typeof(UnityEngine.Object))
                        {
                            continue;
                        }

                        object[] attributes = property.GetCustomAttributes(false);
                        bool isObsoleteWithError = AttributeHelper.IsObsoleteWithError(attributes);
                        if (isObsoleteWithError)
                        {
                            continue;
                        }

                        // Skip properties that cause exceptions at edit time
                        if (Application.isPlaying == false)
                        {
                            if (typeof(MeshFilter).IsAssignableFrom(declaringType))
                            {
                                if (property.Name == "mesh")
                                {
                                    continue;
                                }
                            }

                            if (typeof(Renderer).IsAssignableFrom(declaringType))
                            {
                                if (property.Name == "material" || property.Name == "materials")
                                {
                                    continue;
                                }
                            }
                        }



                        string propertyName = property.Name;

                        MethodInfo getMethod = property.GetGetMethod(true);
                        if (getMethod != null)
                        {
                            object objectValue = getMethod.Invoke(component, null);

                            WrappedVariable wrappedVariable = new WrappedVariable(property, objectValue);
                            description.Properties.Add(wrappedVariable);
                        }
                    }
                }

                if ((flags & InfoFlags.Methods) == InfoFlags.Methods)
                {
                    MethodInfo[] methodInfos = componentType.GetMethods(GetGameObjectRequest.BINDING_FLAGS);
                    foreach (var methodInfo in methodInfos)
                    {
                        if (TypeUtility.IsPropertyMethod(methodInfo, componentType))
                        {
                            // Skip automatically generated getter/setter methods
                            continue;
                        }

                        WrappedMethod wrappedMethod = new WrappedMethod(methodInfo);
                        description.Methods.Add(wrappedMethod);
                    }
                }

                getGOResponse.Components.Add(description);
            }
            return getGOResponse;
        }
    }
}
