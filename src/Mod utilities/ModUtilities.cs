﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace PiTung
{
    /// <summary>
    /// Contains useful methods to make your life easier.
    /// </summary>
    public static class ModUtilities
    {
        /// <summary>
        /// Graphical utilities.
        /// </summary>
        public static GraphicUtilities Graphics { get; } = new GraphicUtilities();
        
        private static readonly IDictionary<KeyValuePair<Type, string>, FieldInfo> FieldCache = new Dictionary<KeyValuePair<Type, string>, FieldInfo>();

        /// <summary>
        /// True if we are one the main menu.
        /// </summary>
        public static bool IsOnMainMenu { get; internal set; } = true;

        /// <summary>
        /// The dummy component, use this to perform tasks that are normally run inside a MonoBehavior,
        /// like coroutines.
        /// </summary>
        public static DummyComponent DummyComponent { get; internal set; }

        /// <summary>
        /// Writes a line to the "output_log.txt" file.
        /// </summary>
        /// <param name="line">The line to be written. May be formatted with {0}, etc.</param>
        /// <param name="args">The arguments.</param>
        public static void Log(string line, params object[] args) => MDebug.WriteLine(line, 0, args: args);

        /// <summary>
        /// Writes a line to the "output_log.txt" file.
        /// </summary>
        /// <param name="line">The line to be written.</param>
        public static void Log(string line) => MDebug.WriteLine(line, 0, new object());

        private static FieldInfo GetField(Type type, string fieldName)
        {
            var key = new KeyValuePair<Type, string>(type, fieldName);

            if (!FieldCache.ContainsKey(key))
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                FieldCache[key] = field ??
                    throw new ArgumentException($"Field {fieldName} not found in object of type {type.Name}.");
            }

            return FieldCache[key];
        }

        /// <summary>
        /// Sets <paramref name="fieldName"/>'s value in <paramref name="obj"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object that has the field we want to change.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The new value for the field.</param>
        /// <param name="isPrivate">True if the field is private.</param>
        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            var field = GetField(obj.GetType(), fieldName);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                throw new ArgumentException($"Field '{fieldName}' not found in {obj.GetType().Name}.", nameof(fieldName));
            }
        }
        
        /// <summary>
        /// Sets the static field <paramref name="fieldName"/>'s value to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TParent">The type that contains <paramref name="fieldName"/>.</typeparam>
        /// <param name="fieldName">The field's name.</param>
        /// <param name="value">The field's new value.</param>
        public static void SetStaticFieldValue<TParent>(string fieldName, object value)
        {
            SetStaticFieldValue(typeof(TParent), fieldName, value);
        }

        /// <summary>
        /// Sets the type <paramref name="parentType"/>'s static field <paramref name="fieldName"/>'s value to <paramref name="value"/>.
        /// </summary>
        /// <param name="parentType">The type that contains <paramref name="fieldName"/>.</param>
        /// <param name="fieldName">The field's name.</param>
        /// <param name="value">The field's new value.</param>
        public static void SetStaticFieldValue(Type parentType, string fieldName, object value)
        {
            var field = GetField(parentType, fieldName);

            field.SetValue(null, value);
        }

        /// <summary>
        /// Gets <paramref name="fieldName"/>'s value in <paramref name="obj"/>.
        /// </summary>
        /// <typeparam name="T">The field's type.</typeparam>
        /// <param name="obj">The object that contains the field.</param>
        /// <param name="fieldName">The field's name.</param>
        /// <param name="isPrivate">True if the field's private.</param>
        /// <returns>The value of the field.</returns>
        public static T GetFieldValue<T>(object obj, string fieldName)
        {
            FieldInfo field = GetField(obj.GetType(), fieldName);

            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// Gets the static field <paramref name="fieldName"/>'s value in <typeparamref name="TParent"/>.
        /// </summary>
        /// <typeparam name="TParent">The type that contains the field.</typeparam>
        /// <typeparam name="TField">The field's type.</typeparam>
        /// <param name="fieldName">The name of the field.</param>
        public static TField GetStaticFieldValue<TParent, TField>(string fieldName)
        {
            return GetStaticFieldValue<TField>(typeof(TParent), fieldName);
        }

        /// <summary>
        /// Gets the static field <paramref name="fieldName"/>'s value in <typeparamref name="parentType"/>.
        /// </summary>
        /// <typeparam name="TField">The field's type.</typeparam>
        /// <param name="parentType">The type that contains the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        public static TField GetStaticFieldValue<TField>(Type parentType, string fieldName)
        {
            FieldInfo field = GetField(parentType, fieldName);

            return (TField)field.GetValue(null);
        }

        /// <summary>
        /// Executes <paramref name="onObject"/>.<paramref name="methodName"/>
        /// </summary>
        /// <param name="onObject">The object that contains the method.</param>
        /// <param name="methodName">The method's name.</param>
        /// <param name="parameters">The method's parameters.</param>
        /// <exception cref="ArgumentException">Throws if the method doesn't exist.</exception>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="methodName"/> or <paramref name="onObject"/> is null.</exception>
        public static void ExecuteMethod(object onObject, string methodName, params object[] parameters)
        {
            if (onObject == null) throw new ArgumentNullException(nameof(onObject));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            Type type = onObject.GetType();
            
            type.InvokeMember(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, onObject, parameters);
        }

        /// <summary>
        /// Executes the static method <paramref name="methodName"/> in <typeparamref name="T"/> with <paramref name="parameters"/> arguments.
        /// </summary>
        /// <typeparam name="T">The type that contains <paramref name="methodName"/>.</typeparam>
        /// <param name="methodName">The method's name.</param>
        /// <param name="parameters">The arguments we want to call the method with.</param>
        /// <exception cref="ArgumentException">Throws if the method doesn't exist.</exception>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="methodName"/> is null.</exception>
        /// <returns>The method's returned value, or null if it doesn't have any.</returns>
        public static object ExecuteStaticMethod<T>(string methodName, params object[] parameters)
        {
            return ExecuteStaticMethod(typeof(T), methodName, parameters);
        }

        /// <summary>
        /// Executes the static method <paramref name="methodName"/> in <paramref name="parentType"/> with <paramref name="parameters"/> arguments.
        /// </summary>
        /// <param name="parentType">The type that contains <paramref name="methodName"/>.</param>
        /// <param name="methodName">The method's name.</param>
        /// <param name="parameters">The arguments we want to call the method with.</param>
        /// <exception cref="ArgumentException">Throws if the method doesn't exist.</exception>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="methodName"/> is null.</exception>
        /// <returns>The method's returned value, or null if it doesn't have any.</returns>
        public static object ExecuteStaticMethod(Type parentType, string methodName, params object[] parameters)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            
            return parentType.InvokeMember(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, parameters);
        }

        /// <summary>
        /// Gets the object that the player possesses.
        /// </summary>
        public static GameObject PlayerObject => GameObject.Find("FPSController").gameObject;
    }
}
