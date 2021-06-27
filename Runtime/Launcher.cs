/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2021/6/25 2:28:35
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace XMLib
{
    /// <summary>
    /// Launcher
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        [SerializeField]
        protected LaunchMode _mode = LaunchMode.Normal;

        [SerializeField]
        protected string _appInitializerName;

        protected void Awake()
        {
            if (_mode != LaunchMode.Normal && App.app != null)
            {
                Destroy(gameObject);
                return;
            }

            Type type = Type.GetType(_appInitializerName, true, true);
            IAppInitializer appInitializer = Activator.CreateInstance(type) as IAppInitializer;
            App.Initialize(appInitializer, _mode);
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(Launcher))]
    public class LaunchEditor : Editor
    {
        private SerializedProperty _mode;
        private SerializedProperty _appInitializerName;
        private string[] _typeNames;
        private List<Type> types;
        private int selectedIndex;

        private void OnEnable()
        {
            types = AssemblyUtility.FindAllAssignable<IAppInitializer>();
            _mode = serializedObject.FindProperty("_mode");
            _appInitializerName = serializedObject.FindProperty("_appInitializerName");
            _typeNames = types.Select(t => t.GetTypeName()).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mode, new GUIContent("启动模式"));

            if (_typeNames.Length > 0)
            {
                selectedIndex = EditorGUILayout.Popup("初始化类", selectedIndex, _typeNames);
                if (selectedIndex >= 0 && selectedIndex < _typeNames.Length)
                {
                    _appInitializerName.stringValue = _typeNames[selectedIndex];
                }
                else
                {
                    _appInitializerName.stringValue = String.Empty;
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"工程中没有发现 继承 {nameof(IAppInitializer)} 接口的初始化类型", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}