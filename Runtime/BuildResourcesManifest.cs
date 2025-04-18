﻿#if UNITY_EDITOR
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Radish.AssetManagement
{
    public sealed class BuildResourcesManifest : ScriptableObject, ISerializationCallbackReceiver
    {
        [DebuggerDisplay("{guid} ({type})", Name = "{path}")]
        [Serializable]
        private class Entry : ISerializationCallbackReceiver, IBuildResourcesManifestEntry
        {
            [SerializeField] private string m_Guid;
            [SerializeField] private string m_Path;
            [SerializeField] private string m_TypeString;

            public string guid => m_Guid;
            public string path => m_Path;
            public Type type { get; private set; }

            public Entry(string guid, string path, Type type)
            {
                m_Guid = guid;
                m_Path = path;
                this.type = type;
            }
            
            public void OnBeforeSerialize()
            {
                m_TypeString = type?.AssemblyQualifiedName ?? string.Empty;
            }

            public void OnAfterDeserialize()
            {
                type = !string.IsNullOrEmpty(m_TypeString) ? Type.GetType(m_TypeString) : null;
            }

            public override string ToString()
            {
                return $"{path} @ {guid} ({type.FullName})";
            }
        }

        public static BuildResourcesManifest instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBoot()
        {
            instance = Load();
        }
        
        [PublicAPI]
        public static BuildResourcesManifest Load()
        {
            #if UNITY_EDITOR
            if (Application.isEditor)
            {
                return Create();
            }
            #endif

            return Resources.Load<BuildResourcesManifest>(nameof(BuildResourcesManifest));
        }

        #if UNITY_EDITOR
        [PublicAPI]
        internal static BuildResourcesManifest Create()
        {
            var manifest = CreateInstance<BuildResourcesManifest>();
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType == typeof(DefaultAsset) || assetType == typeof(BuildResourcesManifest) || assetType == typeof(BuildScenesManifest))
                    continue;

                var pathElements = Path.ChangeExtension(assetPath, null).Split('/').ToList();
                var resIdx = pathElements.FindIndex(x => string.Equals(x, "Resources", StringComparison.InvariantCulture));
                if (resIdx >= 0)
                {
                    var resourcesPath = string.Join('/', pathElements.GetRange((resIdx + 1)..));
                    manifest.m_Entries.Add(new Entry(AssetDatabase.AssetPathToGUID(assetPath), resourcesPath, assetType));
                }
            }

            manifest.OnAfterDeserialize();
            return manifest;
        }
        #endif

        [SerializeField] private List<Entry> m_Entries = new();
        private readonly Dictionary<string, Entry> m_EntryLookup = new();

        [PublicAPI]
        public string GetResourcePathForAsset<T>(SoftAssetReference<T> asset) where T : Object
        {
            if (m_EntryLookup.TryGetValue(asset.guid, out var entry))
            {
                if (!typeof(T).IsAssignableFrom(entry.type))
                {
                    Debug.LogWarningFormat(this, "Asset in manifest was of type {0}, which is not a subclass of {1}", entry.type.FullName, typeof(T).FullName);
                    return string.Empty;
                }

                return entry.path;
            }
            
            return string.Empty;
        }

        [PublicAPI]
        public IEnumerable<IBuildResourcesManifestEntry> GetResourcesInBuild<T>() where T : Object
        {
            return m_Entries.Where(x => x.type != null && x.type.IsSubclassOf(typeof(T)) || x.type == typeof(T));
        }

        [PublicAPI]
        public bool ContainsAssetPath(string path)
        {
            foreach (var e in m_Entries)
            {
                if (e.path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_EntryLookup.Clear();
            foreach (var e in m_Entries)
            {
                m_EntryLookup.Add(e.guid, e);
            }
        }
    }
}