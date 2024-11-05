using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Radish.AssetManagement
{
    [Serializable]
    public sealed class SoftAssetReference<T> : ISoftObjectReference where T : Object
    {
        [SerializeField] private string m_Guid;

        public string guid => m_Guid;
        public bool isValid => !string.IsNullOrEmpty(m_Guid);
        public string path => BuildResourcesManifest.instance.GetResourcePathForAsset(this);

        public ScopedAsyncOperation<T> LoadAsync()
        {
            if (!isValid)
            {
                throw new InvalidOperationException("Reference is not valid");
            }

            var p = path;
            if (string.IsNullOrEmpty(path))
                throw new ResourceNotFoundException(m_Guid, typeof(T));

            var op = ScopedAsyncOperation<T>.Create(out var token);

            var r = Resources.LoadAsync<T>(p);
            r.completed += _ =>
            {
                token.Complete(r.asset as T);
            };

            return op;
        }

        public override string ToString()
        {
#if !UNITY_EDITOR
            return string.Format("{0} (Scene)", m_Guid);
#else
            return string.Format("{0} (Scene)", UnityEditor.AssetDatabase.GUIDToAssetPath(m_Guid));
#endif
        }
    }
}